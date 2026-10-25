using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NileCore.Raar.Core.Extensions;
using Xunit;

namespace NileCore.Raar.Core.Tests
{
    public class CollectionAndDateTimeExtensionsTests
    {
        public enum TestStatus
        {
            [Description("Pending Approval")]
            Pending,
            [Description("Approved and Verified")]
            Approved,
            Rejected
        }

        [Fact]
        public void CollectionExtensions_ForEach_Executes_Action()
        {
            var items = new List<int> { 1, 2, 3 };
            int sum = 0;
            items.ForEach(x => sum += x);
            Assert.Equal(6, sum);
        }

        [Fact]
        public void CollectionExtensions_Batch_Splits_Properly()
        {
            var numbers = Enumerable.Range(1, 10).ToList();
            var batches = numbers.Batch(3).ToList();

            Assert.Equal(4, batches.Count);
            Assert.Equal(new[] { 1, 2, 3 }, batches[0]);
            Assert.Equal(new[] { 4, 5, 6 }, batches[1]);
            Assert.Equal(new[] { 7, 8, 9 }, batches[2]);
            Assert.Equal(new[] { 10 }, batches[3]);
        }

        [Fact]
        public void CollectionExtensions_DistinctBy_Deduplicates()
        {
            var list = new[]
            {
                new { Id = 1, Name = "A" },
                new { Id = 2, Name = "B" },
                new { Id = 1, Name = "C" }
            };

            var distinct = NileCore.Raar.Core.Extensions.CollectionExtensions.DistinctBy(list, x => x.Id).ToList();
            Assert.Equal(2, distinct.Count);
            Assert.Equal("A", distinct[0].Name);
            Assert.Equal("B", distinct[1].Name);
        }

        [Fact]
        public void CollectionExtensions_IsNullOrEmpty_Works()
        {
            List<int>? nullList = null;
            Assert.True(nullList.IsNullOrEmpty());
            Assert.True(new List<int>().IsNullOrEmpty());
            Assert.False(new List<int> { 1 }.IsNullOrEmpty());
        }

        [Fact]
        public void DateTimeExtensions_UnixTimestamp_Conversions()
        {
            var date = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
            long unix = date.ToUnixTimestamp();

            var restored = DateTimeExtensions.FromUnixTimestamp(unix);
            Assert.Equal(date, restored);
        }

        [Fact]
        public void DateTimeExtensions_StartAndEndOfDay()
        {
            var date = new DateTime(2026, 10, 25, 15, 30, 45);
            var start = date.StartOfDay();
            var end = date.EndOfDay();

            Assert.Equal(new DateTime(2026, 10, 25, 0, 0, 0), start);
            Assert.Equal(new DateTime(2026, 10, 25, 23, 59, 59, 999).Date, end.Date);
        }

        [Fact]
        public void EnumExtensions_GetDescription_And_Parse()
        {
            Assert.Equal("Pending Approval", TestStatus.Pending.GetDescription());
            Assert.Equal("Approved and Verified", TestStatus.Approved.GetDescription());
            Assert.Equal("Rejected", TestStatus.Rejected.GetDescription());

            Assert.Equal(TestStatus.Approved, EnumExtensions.Parse<TestStatus>("Approved"));
            Assert.Equal(TestStatus.Approved, EnumExtensions.Parse<TestStatus>("approved", ignoreCase: true));
            Assert.Equal(TestStatus.Pending, EnumExtensions.Parse<TestStatus>("UnknownVal", defaultValue: TestStatus.Pending));

            var all = EnumExtensions.ToList<TestStatus>();
            Assert.Equal(3, all.Count);
        }
    }
}
