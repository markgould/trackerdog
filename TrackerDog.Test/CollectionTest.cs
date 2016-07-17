﻿namespace TrackerDog.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using TrackerDog.Configuration;

    [TestClass]
    public class CollectionTest
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            TrackerDogConfiguration.TrackTheseTypes
            (
                Track.ThisType<A>().IncludeProperty(a => a.Items),
                Track.ThisType<B>().IncludeProperty(b => b.Dogs),
                Track.ThisType<C>().IncludeProperty(c => c.Dogs),
                Track.ThisType<Dog>().IncludeProperty(d => d.Name),
                Track.ThisType<D>(),
                Track.ThisType<E>().IncludeProperty(e => e.Dictionary),
                Track.ThisType<F>().IncludeProperties(f => f.ListOfF),
                Track.ThisType<G>().IncludeProperties(g => g.Buffer),
                Track.ThisType<WhateverBase>().IncludeProperties(b => b.List2),
                Track.ThisType<WhateverParent>().IncludeProperties(d => d.List)
            );
        }

        public class Whatever
        {
        }

        public class WhateverParent : WhateverBase
        {
            public virtual IList<Whatever> List { get; set; } = new List<Whatever>();
        }
        public class WhateverBase
        {
            public virtual IList<string> List2 { get; set; } = new List<string>();
        }

        [DebuggerDisplay("{Name}")]
        public class Dog : IEquatable<Dog>
        {
            public virtual string Name { get; set; }

            public bool Equals(Dog other) =>
                other != null && other.Name == Name;

            public override bool Equals(object obj) =>
                Equals(obj as Dog);

            public override int GetHashCode() => Name.GetHashCode();
        }

        public class A
        {
            public virtual ICollection<string> Items { get; set; } = new List<string>
            {
                "item1", "item2", "item3"
            };
        }

        public class B
        {
            public virtual ICollection<Dog> Dogs { get; set; } = new List<Dog>
            {
                new Dog { Name = "Doggy" },
                new Dog { Name = "Bobby" }
            };
        }

        public class C
        {
            public virtual ISet<Dog> Dogs { get; set; } = new HashSet<Dog>
            {
                new Dog { Name = "Doggy" },
                new Dog { Name = "Bobby" }
            };
        }

        public class D
        {
            public virtual BitArray Mask { get; set; } = new BitArray(2);
        }

        public class E
        {
            public virtual IDictionary<string, string> Dictionary { get; set; } = new Dictionary<string, string>();
        }

        public class F
        {
            public virtual IList<F> ListOfF { get; set; } = new List<F>();
        }

        public class G
        {
            public virtual byte[] Buffer { get; set; }
        }

        [TestMethod]
        public void TrackingTypeWithNonTrackableCollectionWontCrash()
        {
            D d = new D().AsTrackable();
            d.Mask = new BitArray(38);

            Assert.IsTrue(d.PropertyHasChanged(o => o.Mask));
        }

        [TestMethod]
        public void CanTrackCollectionPropertiesOfNonTrackableTypes()
        {
            WhateverParent parent = new WhateverParent().AsTrackable();
            parent.List2.Add("hey");
            parent.List.Add(new Whatever());

            Assert.AreEqual(2, parent.GetChangeTracker().ChangedProperties.Count);
        }

        [TestMethod]
        public void CollectionItemsArePreservedWhenTurningParentObjectIntoTrackable()
        {
            A a = new A().AsTrackable();

            Assert.AreEqual(3, a.Items.Count);

            WhateverParent parent = new WhateverParent();
            parent.List.Add(new Whatever());
            parent.List2.Add("hey");

            parent = parent.AsTrackable();

            Assert.AreEqual(1, parent.List.Count);
            Assert.AreEqual(1, parent.List2.Count);
        }

        [TestMethod]
        public void CanTrackListPropertyChanges()
        {
            A a = new A().AsTrackable();
            a.Items.Add("hola");

            B b = new B().AsTrackable();
            b.Dogs.First().Name = "Rex";
            b.Dogs.Add(new Dog { Name = "Rex" });

            C c = new C().AsTrackable();
            c.Dogs.Add(new Dog { Name = "Rex" });

            IObjectChangeTracker Atracking = a.GetChangeTracker();
            IObjectChangeTracker Btracking = b.GetChangeTracker();
            IObjectChangeTracker Ctracking = c.GetChangeTracker();

            Assert.AreEqual(1, Atracking.ChangedProperties.Count);
            Assert.AreEqual(1, Btracking.ChangedProperties.Count);
            Assert.AreEqual(1, Ctracking.ChangedProperties.Count);
        }

        [TestMethod]
        public void CanTurnCollectionItemsToUntracked()
        {
            B b = new B().AsTrackable();
            b.Dogs.First().Name = "Rex";
            b.Dogs.Add(new Dog { Name = "Rex" });

            b = b.ToUntracked();

            Assert.IsFalse(b.IsTrackable());
            Assert.IsFalse(b.Dogs.Any(dog => dog.IsTrackable()));
        }

        [TestMethod]
        public void CanTrackSetPropertyChanges()
        {
            C c = new C().AsTrackable();

            IObjectChangeTracker Ctracking = c.GetChangeTracker();
            IReadOnlyChangeTrackableCollection trackableCollection = (IReadOnlyChangeTrackableCollection)c.Dogs;

            c.Dogs.Add(new Dog { Name = "Rex" });

            Assert.AreEqual(1, Ctracking.ChangedProperties.Count);
        }

        [TestMethod]
        public void CanTrackSetIntersections()
        {
            C c = new C().AsTrackable();
            c.Dogs.Add(new Dog { Name = "Rex" });

            IReadOnlyChangeTrackableCollection trackableCollection = (IReadOnlyChangeTrackableCollection)c.Dogs;

            c.Dogs.IntersectWith(new[] { new Dog { Name = "Rex" } });

            Assert.AreEqual(1, c.Dogs.Count);
            Assert.AreEqual(2, trackableCollection.RemovedItems.Count);
            Assert.AreEqual(1, trackableCollection.AddedItems.Count);
        }

        [TestMethod]
        public void CanTrackSetExcept()
        {
            C c = new C().AsTrackable();
            c.Dogs.Add(new Dog { Name = "Rex" });

            IReadOnlyChangeTrackableCollection trackableCollection = (IReadOnlyChangeTrackableCollection)c.Dogs;

            c.Dogs.IntersectWith(new[] { new Dog { Name = "Rex" } });
            Assert.AreEqual(1, c.Dogs.Count);
            Assert.AreEqual(2, trackableCollection.RemovedItems.Count);
            Assert.AreEqual(1, trackableCollection.AddedItems.Count);

            c.Dogs.ExceptWith(new[] { new Dog { Name = "Rex" } });
            Assert.AreEqual(0, c.Dogs.Count);
            Assert.AreEqual(2, trackableCollection.RemovedItems.Count);
            Assert.AreEqual(0, trackableCollection.AddedItems.Count);
        }

        [TestMethod]
        public void CanTrackDictionaryChanges()
        {
            E e = new E();
            e.Dictionary.Add("hello", "world");
            e = e.AsTrackable();
            e.Dictionary.Add("bye", "bye");

            Assert.IsTrue(e.PropertyHasChanged(o => o.Dictionary));
        }

        [TestMethod]
        public void CanTrackChangesOfCollectionOfEnclosingType()
        {
            F f = new F().AsTrackable();

            f.ListOfF.Add(new F());

            Assert.IsTrue(f.PropertyHasChanged(o => o.ListOfF));
            Assert.AreEqual(1, ((IReadOnlyChangeTrackableCollection)f.ListOfF).AddedItems.Count);
        }

        [TestMethod]
        public void ArrayMustNotBeTrackedAsCollections()
        {
            G g = new G().AsTrackable();
            g.Buffer = new byte[] { };
            
            Assert.IsTrue(g.PropertyHasChanged(o => o.Buffer));
            Assert.IsNotInstanceOfType(g.Buffer, typeof(IReadOnlyChangeTrackableCollection));
        }
    }
}