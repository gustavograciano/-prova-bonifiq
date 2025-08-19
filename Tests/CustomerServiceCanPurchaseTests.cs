using Microsoft.EntityFrameworkCore;
using Moq;
using ProvaPub.Models;
using ProvaPub.Repository;
using ProvaPub.Services;
using Xunit;

namespace ProvaPub.Tests
{
    public class CustomerServiceCanPurchaseTests
    {
        private TestDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new TestDbContext(options);
        }

        [Fact]
        public async Task CanPurchase_CustomerIdZeroOrNegative_ThrowsArgumentOutOfRangeException()
        {
            using var context = GetInMemoryContext();
            var mockDateTimeProvider = new Mock<IDateTimeProvider>();
            var service = new CustomerService(context, mockDateTimeProvider.Object);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CanPurchase(0, 50));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CanPurchase(-1, 50));
        }

        [Fact]
        public async Task CanPurchase_PurchaseValueZeroOrNegative_ThrowsArgumentOutOfRangeException()
        {
            using var context = GetInMemoryContext();
            var mockDateTimeProvider = new Mock<IDateTimeProvider>();
            var service = new CustomerService(context, mockDateTimeProvider.Object);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CanPurchase(1, 0));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CanPurchase(1, -10));
        }

        [Fact]
        public async Task CanPurchase_CustomerDoesNotExist_ThrowsInvalidOperationException()
        {
            using var context = GetInMemoryContext();
            var mockDateTimeProvider = new Mock<IDateTimeProvider>();
            var service = new CustomerService(context, mockDateTimeProvider.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CanPurchase(999, 50));
            Assert.Contains("Customer Id 999 does not exists", exception.Message);
        }

        [Fact]
        public async Task CanPurchase_CustomerHasOrderInCurrentMonth_ReturnsFalse()
        {
            using var context = GetInMemoryContext();
            var mockDateTime = new Mock<IDateTimeProvider>();
            var currentDate = new DateTime(2023, 6, 15, 10, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(x => x.UtcNow).Returns(currentDate);

            var customer = new Customer { Id = 1, Name = "Test Customer" };
            context.Customers.Add(customer);

            var recentOrder = new Order
            {
                Id = 1,
                CustomerId = 1,
                Value = 50,
                OrderDate = currentDate.AddDays(-10)
            };
            context.Orders.Add(recentOrder);
            await context.SaveChangesAsync();

            var service = new CustomerService(context, mockDateTime.Object);

            var result = await service.CanPurchase(1, 75);

            Assert.False(result);
        }

        [Fact]
        public async Task CanPurchase_FirstTimeBuyerWithValueOver100_ReturnsFalse()
        {
            using var context = GetInMemoryContext();
            var mockDateTime = new Mock<IDateTimeProvider>();
            var currentDate = new DateTime(2023, 6, 15, 10, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(x => x.UtcNow).Returns(currentDate);

            var customer = new Customer { Id = 1, Name = "New Customer" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerService(context, mockDateTime.Object);

            var result = await service.CanPurchase(1, 150);

            Assert.False(result);
        }

        [Fact]
        public async Task CanPurchase_FirstTimeBuyerWithValue100OrLess_ReturnsTrue()
        {
            using var context = GetInMemoryContext();
            var mockDateTime = new Mock<IDateTimeProvider>();
            var currentDate = new DateTime(2023, 6, 15, 10, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(x => x.UtcNow).Returns(currentDate);

            var customer = new Customer { Id = 1, Name = "New Customer" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerService(context, mockDateTime.Object);

            var result = await service.CanPurchase(1, 100);

            Assert.True(result);
        }

        [Theory]
        [InlineData(7, false)]  // Before business hours
        [InlineData(8, true)]   // Start of business hours
        [InlineData(12, true)]  // During business hours
        [InlineData(18, true)]  // End of business hours
        [InlineData(19, false)] // After business hours
        public async Task CanPurchase_BusinessHoursValidation_ReturnsExpectedResult(int hour, bool expected)
        {
            using var context = GetInMemoryContext();
            var mockDateTime = new Mock<IDateTimeProvider>();
            var currentDate = new DateTime(2023, 6, 15, hour, 0, 0, DateTimeKind.Utc); // Thursday
            mockDateTime.Setup(x => x.UtcNow).Returns(currentDate);

            var customer = new Customer { Id = 1, Name = "Test Customer" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerService(context, mockDateTime.Object);

            var result = await service.CanPurchase(1, 50);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(DayOfWeek.Monday, true)]
        [InlineData(DayOfWeek.Tuesday, true)]
        [InlineData(DayOfWeek.Wednesday, true)]
        [InlineData(DayOfWeek.Thursday, true)]
        [InlineData(DayOfWeek.Friday, true)]
        [InlineData(DayOfWeek.Saturday, false)]
        [InlineData(DayOfWeek.Sunday, false)]
        public async Task CanPurchase_WeekdayValidation_ReturnsExpectedResult(DayOfWeek dayOfWeek, bool expected)
        {
            using var context = GetInMemoryContext();
            var mockDateTime = new Mock<IDateTimeProvider>();
            
            var baseDate = new DateTime(2023, 6, 12); // Monday
            var testDate = baseDate.AddDays((int)dayOfWeek - (int)DayOfWeek.Monday);
            var currentDate = new DateTime(testDate.Year, testDate.Month, testDate.Day, 10, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(x => x.UtcNow).Returns(currentDate);

            var customer = new Customer { Id = 1, Name = "Test Customer" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerService(context, mockDateTime.Object);

            var result = await service.CanPurchase(1, 50);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CanPurchase_ValidScenario_ReturnsTrue()
        {
            using var context = GetInMemoryContext();
            var mockDateTime = new Mock<IDateTimeProvider>();
            var currentDate = new DateTime(2023, 6, 15, 10, 0, 0, DateTimeKind.Utc); // Thursday 10 AM
            mockDateTime.Setup(x => x.UtcNow).Returns(currentDate);

            var customer = new Customer { Id = 1, Name = "Valid Customer" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerService(context, mockDateTime.Object);

            var result = await service.CanPurchase(1, 75);

            Assert.True(result);
        }

        [Fact]
        public async Task CanPurchase_CustomerWithOldOrder_ReturnsTrue()
        {
            using var context = GetInMemoryContext();
            var mockDateTime = new Mock<IDateTimeProvider>();
            var currentDate = new DateTime(2023, 6, 15, 10, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(x => x.UtcNow).Returns(currentDate);

            var customer = new Customer { Id = 1, Name = "Returning Customer" };
            context.Customers.Add(customer);

            var oldOrder = new Order
            {
                Id = 1,
                CustomerId = 1,
                Value = 50,
                OrderDate = currentDate.AddMonths(-2)
            };
            context.Orders.Add(oldOrder);
            await context.SaveChangesAsync();

            var service = new CustomerService(context, mockDateTime.Object);

            var result = await service.CanPurchase(1, 200);

            Assert.True(result);
        }

        [Fact]
        public async Task CanPurchase_ReturningCustomerWithHighValue_ReturnsTrue()
        {
            using var context = GetInMemoryContext();
            var mockDateTime = new Mock<IDateTimeProvider>();
            var currentDate = new DateTime(2023, 6, 15, 10, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(x => x.UtcNow).Returns(currentDate);

            var customer = new Customer { Id = 1, Name = "Returning Customer" };
            customer.Orders = new List<Order>
            {
                new Order { Id = 1, CustomerId = 1, Value = 50, OrderDate = currentDate.AddMonths(-3) }
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new CustomerService(context, mockDateTime.Object);

            var result = await service.CanPurchase(1, 500);

            Assert.True(result);
        }
    }
}