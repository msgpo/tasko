﻿using System;
using System.Threading;
using NUnit.Framework;
using Tasko.Server.Models;

namespace Tasko.Tests.Unit
{
    public class TaskTests
    {
        private Task task;
        private DateTime lastEditedAt;

        [SetUp]
        public void Setup()
        {
            this.task = new Task("foo", "cat1");

            // save value for tests (to be able to tell if the value was changed after creation)
            this.lastEditedAt = this.task.LastEditedAt;
        }

        /// <summary>
        /// helper method: pause for a few ms
        /// </summary>
        /// <remarks>
        /// To make sure that the "last edited" value is actually different when set a second time.
        /// </remarks>
        public void Wait()
        {
            Thread.Sleep(30);
        }

        [TestFixture]
        public class Constructor : TaskTests
        {
            [Test]
            public void DescriptionWasSet()
            {
                Assert.AreEqual("foo", task.Description);
            }

            [Test]
            public void ThrowsWhenDescriptionIsEmpty()
            {
                Assert.Catch(() => new Task("", "cat1"));
            }

            [Test]
            public void CategoryWasAdded()
            {
                Assert.AreEqual(1, task.Categories.Count);
                Assert.AreEqual("cat1", task.Categories[0]);
            }

            [Test]
            public void ThrowsWhenCategoryIsEmpty()
            {
                Assert.Catch(() => new Task("foo", ""));
            }

            [Test]
            public void CreatedAtWasSet()
            {
                Assert.AreNotEqual(DateTime.MinValue, task.CreatedAt);
            }

            [Test]
            public void LastEditedWasSet()
            {
                Assert.AreNotEqual(DateTime.MinValue, task.LastEditedAt);
            }

            [Test]
            public void TaskIsNotFinished()
            {
                Assert.IsFalse(task.IsFinished);
            }
        }

        [TestFixture]
        public class Description : TaskTests
        {
            [Test]
            public void ThrowsWhenEmpty()
            {
                Assert.Catch(() => task.Description = null);
                Assert.Catch(() => task.Description = "");
                Assert.Catch(() => task.Description = "   ");
            }

            [Test]
            public void LastEditedChangesWhenDescriptionIsChanged()
            {
                Wait();
                task.Description = "bar";
                Assert.AreNotEqual(this.lastEditedAt, task.LastEditedAt);
            }
        }

        [TestFixture]
        public class AddCategory : TaskTests
        {
            [Test]
            public void CategoryWasAdded()
            {
                int numberOfCategories = task.Categories.Count;
                task.AddCategory("cat2");

                Assert.AreEqual(numberOfCategories + 1, task.Categories.Count);
                Assert.That(task.Categories.Contains("cat2"));
            }

            [Test]
            public void ThrowsWhenCategoryIsEmpty()
            {
                Assert.Catch(() => task.AddCategory(null));
                Assert.Catch(() => task.AddCategory(""));
                Assert.Catch(() => task.AddCategory("   "));
            }

            [Test]
            public void LastEditedChangesWhenCategoryIsAdded()
            {
                Wait();
                task.AddCategory("cat2");
                Assert.AreNotEqual(this.lastEditedAt, task.LastEditedAt);
            }
        }

        [TestFixture]
        public class Finish : TaskTests
        {
            [Test]
            public void TaskIsFinished()
            {
                task.Finish();
                Assert.That(task.IsFinished);
            }

            [Test]
            public void FinishedAtIsSet()
            {
                task.Finish();
                Assert.That(task.FinishedAt.HasValue);
            }

            [Test]
            public void LastEditedIsSet()
            {
                Wait();
                task.Finish();
                Assert.AreNotEqual(this.lastEditedAt, task.LastEditedAt);
            }

            [Test]
            public void ThrowsWhenTaskIsFinishedTwice()
            {
                task.Finish();
                Assert.Catch(() => task.Finish());
            }
        }

        [TestFixture]
        public class Reopen : TaskTests
        {
            [Test]
            public void TaskIsNotFinished()
            {
                task.Finish();
                task.Reopen();

                Assert.That(!task.IsFinished);
            }

            [Test]
            public void FinishedAtIsNull()
            {
                task.Finish();
                task.Reopen();

                Assert.That(!task.FinishedAt.HasValue);
            }

            [Test]
            public void LastEditedIsSet()
            {
                task.Finish();
                
                // save LastEdited again and wait (to make sure that the value is different)
                this.lastEditedAt = this.task.LastEditedAt;
                Wait();

                task.Reopen();

                Assert.AreNotEqual(this.lastEditedAt, task.LastEditedAt);
            }

            [Test]
            public void ThrowsWhenUnfinishedTaskIsReopened()
            {
                Assert.Catch(() => task.Reopen());
            }

            [Test]
            public void ThrowsWhenTaskIsReopenedTwice()
            {
                task.Finish();
                task.Reopen();
                Assert.Catch(() => task.Reopen());
            }
        }
    }
}
