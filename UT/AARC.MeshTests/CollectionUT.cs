using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AARC.Mesh.Comparers;
using System.Collections;

namespace AARC.MeshTests
{
    [TestClass]
    public class CollectionUT
    {
        [TestMethod]
        public void TestMySetTheory()
        {
            var porfolioOfTickers = new HashSet<int>(new[] { 0, 2, 4, 6, 8 });
            // Rmoved 0 and added 10
            var folioChange = new HashSet<int>(new[] { 2, 4, 6, 8, 10 });

            var include = porfolioOfTickers.Intersect(folioChange);
            var changeDeleted = porfolioOfTickers.Except(folioChange).ToList();
            var changeAdded = folioChange.Except(porfolioOfTickers).ToList();
            porfolioOfTickers.IntersectWith(include);
            porfolioOfTickers.UnionWith(changeAdded);
            CollectionAssert.AreEqual(changeDeleted, new[] { 0 });
            CollectionAssert.AreEqual(changeAdded, new[] { 10 });
            CollectionAssert.AreEqual(porfolioOfTickers.OrderBy(s => s).ToList(), new[] { 2, 4, 6, 8, 10 });
        }

        [TestMethod]
        public void TestCollectionChangesSame()
        {
            var students = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            //{ 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Andy", "Ruth", "198" } }
        };

            var updated = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            //{ 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Andy", "Ruth", "198" } },
            //{ 114, new List<string> { "Count", "RuDracth", "222" } }
        };
            //Dictionary has TValueCOmpare
            var (added, deleted) = students.Changes<int,List<string>>(updated);

            Assert.IsFalse(added.Keys.Any());
            Assert.IsFalse(deleted.Keys.Any());
        }

        [TestMethod]
        public void TestCollectionValueAdded()
        {
            var students = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            //{ 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Andy", "Ruth", "198" } }
        };

            var updated = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            //{ 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Andy", "Ruth", "198", "New" } },
            //{ 114, new List<string> { "Count", "RuDracth", "222" } }
        };
            //Dictionary has TValueCOmpare
            var (added, deleted) = students.Changes<int, List<string>>(updated);

            CollectionAssert.AreEqual(added.Keys as ICollection, new[] { 113 });
            Assert.IsFalse(deleted.Keys.Any());
        }

        [TestMethod]
        public void TestCollectionValueDeleted()
        {
            var students = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            //{ 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Andy", "Ruth", "198" } }
        };

            var updated = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            //{ 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Ruth", "198" } },
            //{ 114, new List<string> { "Count", "RuDracth", "222" } }
        };
            //Dictionary has TValueCOmpare
            var (added, deleted) = students.Changes<int, List<string>>(updated);

            CollectionAssert.AreEqual(deleted.Keys as ICollection, new[] { 113 });
            Assert.IsFalse(added.Keys.Any());
        }

        [TestMethod]
        public void TestCollectionChangesAdd1()
        {
            var students = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            //{ 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Andy", "Ruth", "198" } }
        };

            var updated = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            //{ 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Andy", "Ruth", "198" } },
            { 114, new List<string> { "Count", "RuDracth", "222" } }
        };
            //Dictionary has TValueCOmpare
            var (added, deleted) = students.Changes<int, List<string>>(updated);

            CollectionAssert.AreEqual(added.Keys as ICollection, new[] { 114 });
        }

        [TestMethod]
        public void TestCollectionChangesAdd1Delete1()
        {
            var students = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            { 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Andy", "Ruth", "198" } }
        };

            var updated = new Dictionary<int, List<string>>()
        {
            //{ 111, new List<string> { "Sachin", "Karnik", "211" } },
            //{ 112, new List<string> { "Dina", "Salimzianova", "317"  } },
            { 113, new List<string> { "Andy", "Ruth", "198" } },
            { 114, new List<string> { "Count", "RuDracth", "222" } }
        };
            //Dictionary has TValueCOmpare
            var (added, deleted) = students.Changes<int, List<string>>(updated);

            CollectionAssert.AreEqual(added.Keys as ICollection, new[] { 114 });
            CollectionAssert.AreEqual(deleted.Keys as ICollection, new[] { 112 });
        }
    }
}
