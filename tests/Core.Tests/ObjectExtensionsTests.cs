using System;
using System.Collections.Generic;
using NileCore.Raar.Core.Extensions;
using Xunit;

namespace NileCore.Raar.Core.Tests
{
    public class ObjectExtensionsTests
    {
        public class UserPoco
        {
            public int Id { get; set; } = 1;
            public string Name { get; set; } = "Osama";
            public bool IsActive { get; set; } = true;
        }

        private class DisposableEnumerable : IEnumerable<int>, IDisposable
        {
            public bool Disposed { get; private set; }

            public IEnumerator<int> GetEnumerator() => new DisposableEnumerator(this);
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

            public void Dispose()
            {
                Disposed = true;
            }

            private class DisposableEnumerator : IEnumerator<int>
            {
                private readonly DisposableEnumerable _parent;
                private bool _first = true;

                public DisposableEnumerator(DisposableEnumerable parent) => _parent = parent;
                public int Current => 42;
                object System.Collections.IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    if (_first)
                    {
                        _first = false;
                        return true;
                    }
                    return false;
                }
                public void Reset() { }
                public void Dispose() => _parent.Dispose();
            }
        }

        [Fact]
        public void ToDictionary_Extracts_Public_Properties()
        {
            var user = new UserPoco { Id = 42, Name = "Ahmed", IsActive = false };
            var dict = user.ToDictionary();

            Assert.Equal(3, dict.Count);
            Assert.Equal(42, dict["Id"]);
            Assert.Equal("Ahmed", dict["Name"]);
            Assert.Equal(false, dict["IsActive"]);
        }

        [Fact]
        public void IsEmpty_Detects_Empty_And_NonEmpty_Types()
        {
            // Null
            string? nullStr = null;
            Assert.True(nullStr.IsEmpty());

            // Strings
            Assert.True("".IsEmpty());
            Assert.True("   ".IsEmpty());
            Assert.False("hello".IsEmpty());

            // Guid
            Assert.True(Guid.Empty.IsEmpty());
            Assert.False(Guid.NewGuid().IsEmpty());

            // Collections
            Assert.True(new List<string>().IsEmpty());
            Assert.False(new List<string> { "item" }.IsEmpty());
            Assert.True(Array.Empty<int>().IsEmpty());
            Assert.False(new int[] { 1 }.IsEmpty());

            // Value types
            Assert.True(0.IsEmpty());
            Assert.False(42.IsEmpty());
            Assert.True(false.IsEmpty());
            Assert.False(true.IsEmpty());
        }

        [Fact]
        public void IsEmpty_Properly_Disposes_Enumerator()
        {
            var disposableSeq = new DisposableEnumerable();
            bool empty = disposableSeq.IsEmpty();

            Assert.False(empty);
            Assert.True(disposableSeq.Disposed, "Enumerator must be disposed to avoid resource leaks!");
        }

        [Fact]
        public void ThrowIfNull_Throws_When_Null_And_Returns_Instance()
        {
            string? nullObj = null;
            Assert.Throws<ArgumentNullException>(() => nullObj.ThrowIfNull("nullObj"));

            string valid = "valid";
            Assert.Same(valid, valid.ThrowIfNull(nameof(valid)));
        }

        [Fact]
        public void Obsolete_ObjectExtenstions_Still_Works_For_Backward_Compatibility()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var user = new UserPoco();
            var dict = ObjectExtenstions.ToDictionary(user);
            Assert.NotNull(dict);
            Assert.False(ObjectExtenstions.IsEmpty(user));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void Map_Transforms_Source_Correctly()
        {
            var user = new UserPoco { Id = 7, Name = "Target" };
            int idTimesTen = user.Map(u => u.Id * 10);
            Assert.Equal(70, idTimesTen);
        }

        [Fact]
        public void Dump_Produces_Readable_Representation()
        {
            var user = new UserPoco { Id = 10, Name = "InspectMe", IsActive = true };
            string dump = user.Dump();
            Assert.Contains("UserPoco", dump);
            Assert.Contains("Id: 10", dump);
            Assert.Contains("Name: InspectMe", dump);
        }
    }
}
