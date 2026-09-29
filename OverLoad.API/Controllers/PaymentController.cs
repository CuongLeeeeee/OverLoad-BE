using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.V2.PaymentRequests.Invoices;
using OverLoad.API.Common;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Data;

namespace OverLoad.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payment")]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PayOSClient _payOS;
    private static readonly System.Threading.SemaphoreSlim _syncSemaphore = new System.Threading.SemaphoreSlim(1, 1);

    public PaymentController(AppDbContext context, PayOSClient payOS)
    {
        _context = context;
        _payOS = payOS;
    }

    private async Task SyncPendingTransactionsAsync(int userId)
    {
        // Kiểm tra nhanh xem người dùng có giao dịch PENDING nào không
        var hasPending = await _context.Transactions.AnyAsync(t => t.UserId == userId && t.Status == "PENDING");
        if (!hasPending) return;

        await _syncSemaphore.WaitAsync();
        try
        {
            // Tải lại danh sách dưới lock để đảm bảo đồng bộ
            var pendingTransactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.Status == "PENDING")
                .ToListAsync();

            if (pendingTransactions.Count == 0) return;

            bool databaseChanged = false;

            foreach (var transaction in pendingTransactions)
            {
                try
                {
                    // Lấy trạng thái mới nhất từ PayOS
                    var paymentLink = await _payOS.PaymentRequests.GetAsync(transaction.OrderCode);
                    if (paymentLink != null)
                    {
                        var statusStr = paymentLink.Status.ToString();
                        if (statusStr.Equals("PAID", StringComparison.OrdinalIgnoreCase) || paymentLink.AmountPaid >= transaction.Amount)
                        {
                            transaction.Status = "SUCCESS";
                            transaction.PaymentTime = DateTime.UtcNow;

                            // Ghi danh (enroll) nếu là mua khóa học hoặc gói PRO
                            var alreadyEnrolled = await _context.Enrollments
                                .AnyAsync(e => e.UserId == transaction.UserId && e.CourseId == transaction.CourseId);

                            if (!alreadyEnrolled)
                            {
                                var enrollment = new Enrollment
                                {
                                    UserId = transaction.UserId,
                                    CourseId = transaction.CourseId,
                                    EnrolledAt = DateTime.UtcNow
                                };
                                _context.Enrollments.Add(enrollment);
                            }

                            databaseChanged = true;
                        }
                        else if (statusStr.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) || statusStr.Equals("CANCELED", StringComparison.OrdinalIgnoreCase))
                        {
                            transaction.Status = "CANCELLED";
                            databaseChanged = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ghi nhận lỗi của riêng giao dịch này để không ảnh hưởng giao dịch khác
                    Console.WriteLine($"Error syncing transaction {transaction.OrderCode}: {ex.Message}");
                }
            }

            if (databaseChanged)
            {
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    /// <summary>Tạo link thanh toán mua khóa học</summary>
    [HttpPost("create-link")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePaymentLink([FromBody] CreateLinkRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var course = await _context.Courses.FindAsync(request.CourseId);
        if (course == null)
            return NotFound(new { message = "Khóa học không tồn tại." });

        if (course.Price <= 0)
            return BadRequest(new { message = "Khóa học này miễn phí, vui lòng đăng ký trực tiếp." });

        var alreadyEnrolled = await _context.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == course.Id);
        if (alreadyEnrolled)
            return BadRequest(new { message = "Bạn đã đăng ký khóa học này rồi." });

        long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Cấu hình URL trả về cho Next.js
        string returnUrl = request.ReturnUrl ?? "http://localhost:3000/payment/success";
        string cancelUrl = request.CancelUrl ?? "http://localhost:3000/payment/cancel";

        try
        {
            var items = new List<PaymentLinkItem>
            {
                new PaymentLinkItem
                {
                    Name = course.Title.Substring(0, Math.Min(25, course.Title.Length)),
                    Quantity = 1,
                    Price = (long)course.Price
                }
            };

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)course.Price,
                Description = $"Mua KH {course.Id}".Substring(0, Math.Min(25, $"Mua KH {course.Id}".Length)),
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                Items = items
            };

            var paymentLinkResult = await _payOS.PaymentRequests.CreateAsync(paymentRequest);

            // Lưu đơn hàng tạm vào DB dưới dạng PENDING
            var transaction = new Transaction
            {
                TransactionId = paymentLinkResult.PaymentLinkId,
                OrderCode = orderCode,
                UserId = userId,
                CourseId = course.Id,
                Amount = course.Price,
                Currency = "VND",
                Status = "PENDING",
                PaymentTime = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { checkoutUrl = paymentLinkResult.CheckoutUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi kết nối cổng thanh toán PayOS.", error = ex.Message });
        }
    }

    /// <summary>Tạo link thanh toán nâng cấp tài khoản PRO</summary>
    [HttpPost("create-pro-link")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProPaymentLink([FromBody] CreateProRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var proCourse = await GetOrCreateProCourseAsync(request.PackageType);
        long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string returnUrl = request.ReturnUrl ?? "http://localhost:3000/payment/success";
        string cancelUrl = request.CancelUrl ?? "http://localhost:3000/payment/cancel";

        try
        {
            var items = new List<PaymentLinkItem>
            {
                new PaymentLinkItem
                {
                    Name = proCourse.Title.Substring(0, Math.Min(25, proCourse.Title.Length)),
                    Quantity = 1,
                    Price = (long)proCourse.Price
                }
            };

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)proCourse.Price,
                Description = $"Pro {request.PackageType}".Substring(0, Math.Min(25, $"Pro {request.PackageType}".Length)),
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                Items = items
            };

            var paymentLinkResult = await _payOS.PaymentRequests.CreateAsync(paymentRequest);

            var transaction = new Transaction
            {
                TransactionId = paymentLinkResult.PaymentLinkId,
                OrderCode = orderCode,
                UserId = userId,
                CourseId = proCourse.Id,
                Amount = proCourse.Price,
                Currency = "VND",
                Status = "PENDING",
                PaymentTime = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { checkoutUrl = paymentLinkResult.CheckoutUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi kết nối cổng thanh toán PayOS.", error = ex.Message });
        }
    }

    /// <summary>Nâng cấp gói PRO sử dụng số dư tài khoản</summary>
    [HttpPost("buy-pro-balance")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BuyProWithBalance([FromBody] CreateProRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await SyncPendingTransactionsAsync(userId);

        var proCourse = await GetOrCreateProCourseAsync(request.PackageType);

        // 1. Tính toán số dư hiện tại của học viên
        var balance = await _context.Transactions
            .Include(t => t.Course)
            .Where(t => t.UserId == userId && t.Status == "SUCCESS" && (t.Course.Slug == "system-deposit-balance" || t.Amount < 0))
            .SumAsync(t => t.Amount);

        if (balance < proCourse.Price)
        {
            return BadRequest(new { message = "Số dư tài khoản không đủ. Vui lòng nạp thêm tiền." });
        }

        // 2. Tạo giao dịch trừ tiền trong ví (lưu dưới dạng số âm)
        long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string transactionId = $"WALLET_PRO_{orderCode}";

        var transaction = new Transaction
        {
            TransactionId = transactionId,
            OrderCode = orderCode,
            UserId = userId,
            CourseId = proCourse.Id,
            Amount = -proCourse.Price, // Số âm biểu thị giao dịch trừ tiền
            Currency = "VND",
            Status = "SUCCESS",
            PaymentTime = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);

        // 3. Tiến hành ghi danh (enroll) học viên vào khóa PRO tương ứng
        var alreadyEnrolled = await _context.Enrollments
            .AnyAsync(e => e.UserId == userId && e.CourseId == proCourse.Id);

        if (!alreadyEnrolled)
        {
            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseId = proCourse.Id,
                EnrolledAt = DateTime.UtcNow
            };
            _context.Enrollments.Add(enrollment);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Nâng cấp tài khoản PRO thành công!" });
    }

    /// <summary>Endpoint webhook nhận thông báo từ PayOS khi thanh toán thành công</summary>
    /// <remarks>
    /// The unversioned /api/payment/webhook route is kept because it is the URL registered in the
    /// PayOS dashboard; remove it once the dashboard points at /api/v1/payment/webhook.
    /// </remarks>
    [HttpPost("webhook")]
    [HttpPost("/api/payment/webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook([FromBody] Webhook webhookBody)
    {
        if (webhookBody == null)
            return BadRequest(new { message = "Dữ liệu webhook rỗng." });

        try
        {
            // Xác thực chữ ký điện tử từ PayOS để chống hack tiền
            var data = await _payOS.Webhooks.VerifyAsync(webhookBody);

            // Tìm transaction tương ứng trong database theo OrderCode
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.OrderCode == data.OrderCode);

            if (transaction == null)
                return NotFound(new { message = $"Không tìm thấy giao dịch với OrderCode: {data.OrderCode}" });

            if (transaction.Status == "SUCCESS")
                return Ok(new { message = "Giao dịch đã được cập nhật thành công trước đó." });

            // Cập nhật trạng thái giao dịch
            transaction.Status = "SUCCESS";
            transaction.PaymentTime = DateTime.UtcNow;

            // Mở khóa khóa học: Chèn dòng vào bảng Enrollments
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UserId == transaction.UserId && e.CourseId == transaction.CourseId);

            if (!alreadyEnrolled)
            {
                var enrollment = new Enrollment
                {
                    UserId = transaction.UserId,
                    CourseId = transaction.CourseId,
                    EnrolledAt = DateTime.UtcNow
                };
                _context.Enrollments.Add(enrollment);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Giao dịch được xác thực và cập nhật thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Chữ ký webhook không hợp lệ hoặc lỗi cập nhật DB.", error = ex.Message });
        }
    }

    /// <summary>Lấy danh sách giao dịch (Người dùng thường chỉ lấy giao dịch của họ, Admin/Giảng viên lấy tất cả)</summary>
    [HttpGet("transactions")]
    [Authorize]
    public async Task<IActionResult> GetTransactions()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await SyncPendingTransactionsAsync(userId);

        var userRoleClaim = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        bool isStaff = userRoleClaim == "Admin" || userRoleClaim == "Instructor";

        var query = _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Course)
            .AsQueryable();

        if (!isStaff)
        {
            query = query.Where(t => t.UserId == userId);
        }

        var transactions = await query
            .OrderByDescending(t => t.PaymentTime)
            .ToListAsync();

        var result = new List<object>();
        foreach (var t in transactions)
        {
            result.Add(new
            {
                transactionId = t.TransactionId,
                orderCode = t.OrderCode,
                userId = t.UserId,
                userFullName = t.User?.FullName ?? "Học viên",
                courseId = t.CourseId,
                courseTitle = t.Course?.Title ?? "Khóa học",
                amount = t.Amount,
                currency = t.Currency,
                status = t.Status,
                paymentTime = t.PaymentTime
            });
        }

        return Ok(result);
    }

    /// <summary>Lấy thông tin thống kê doanh thu</summary>
    [HttpGet("stats")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetStats()
    {
        var successTransactions = await _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Course)
            .Where(t => t.Status == "SUCCESS")
            .OrderByDescending(t => t.PaymentTime)
            .ToListAsync();

        decimal totalRevenue = 0;
        int coursesSold = successTransactions.Count;

        foreach (var t in successTransactions)
        {
            totalRevenue += t.Amount;
        }

        var recentList = new List<object>();
        foreach (var t in successTransactions.Take(10))
        {
            recentList.Add(new
            {
                transactionId = t.TransactionId,
                orderCode = t.OrderCode,
                userFullName = t.User?.FullName ?? "Học viên",
                courseTitle = t.Course?.Title ?? "Khóa học",
                amount = t.Amount,
                paymentTime = t.PaymentTime,
                status = t.Status
            });
        }

        return Ok(new
        {
            totalRevenue = totalRevenue,
            coursesSold = coursesSold,
            transactions = recentList
        });
    }

    private async Task<Course> GetOrCreateProCourseAsync(string packageType)
    {
        string slug = packageType == "year" ? "pro-upgrade-year" : "pro-upgrade-month";
        string title = packageType == "year" ? "Gói nâng cấp PRO - 1 năm" : "Gói nâng cấp PRO - 1 tháng";
        decimal price = packageType == "year" ? 599000 : 69000;

        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Slug == slug);
        if (course == null)
        {
            course = new Course
            {
                Title = title,
                Slug = slug,
                Description = "Nâng cấp tài khoản PRO để học trọn đời tất cả các khóa học trên hệ thống.",
                Price = price,
                Category = "System",
                IsPublished = false
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
        }
        return course;
    }

    /// <summary>Tạo link nạp tiền vào tài khoản</summary>
    [HttpPost("create-deposit-link")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDepositPaymentLink([FromBody] CreateDepositRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (request.Amount < 10000)
            return BadRequest(new { message = "Số tiền nạp tối thiểu là 10.000đ" });

        var depositCourse = await GetOrCreateDepositCourseAsync(request.Amount);
        long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string returnUrl = request.ReturnUrl ?? "http://localhost:3000/payment/success";
        string cancelUrl = request.CancelUrl ?? "http://localhost:3000/payment/cancel";

        try
        {
            var items = new List<PaymentLinkItem>
            {
                new PaymentLinkItem
                {
                    Name = $"Nap tien {request.Amount}d",
                    Quantity = 1,
                    Price = (long)request.Amount
                }
            };

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)request.Amount,
                Description = $"Nap tien {userId}".Substring(0, Math.Min(25, $"Nap tien {userId}".Length)),
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                Items = items
            };

            var paymentLinkResult = await _payOS.PaymentRequests.CreateAsync(paymentRequest);

            var transaction = new Transaction
            {
                TransactionId = paymentLinkResult.PaymentLinkId,
                OrderCode = orderCode,
                UserId = userId,
                CourseId = depositCourse.Id,
                Amount = request.Amount,
                Currency = "VND",
                Status = "PENDING",
                PaymentTime = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { checkoutUrl = paymentLinkResult.CheckoutUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi kết nối cổng thanh toán PayOS.", error = ex.Message });
        }
    }

    /// <summary>Lấy số dư hiện tại của học viên</summary>
    [HttpGet("balance")]
    [Authorize]
    public async Task<IActionResult> GetUserBalance()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await SyncPendingTransactionsAsync(userId);

        var balance = await _context.Transactions
            .Include(t => t.Course)
            .Where(t => t.UserId == userId && t.Status == "SUCCESS" && (t.Course.Slug == "system-deposit-balance" || t.Amount < 0))
            .SumAsync(t => t.Amount);

        return Ok(new { balance = balance });
    }

    private async Task<Course> GetOrCreateDepositCourseAsync(decimal amount)
    {
        string slug = "system-deposit-balance";
        string title = "Nạp tiền vào tài khoản";

        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Slug == slug);
        if (course == null)
        {
            course = new Course
            {
                Title = title,
                Slug = slug,
                Description = "Giao dịch nạp tiền vào số dư tài khoản học viên.",
                Price = amount,
                Category = "System",
                IsPublished = false
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
        }
        return course;
    }
}

public class CreateLinkRequest
{
    public int CourseId { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class CreateProRequest
{
    public string PackageType { get; set; } = "month"; // month or year
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class CreateDepositRequest
{
    public decimal Amount { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}
