﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using NUnit.Framework;
using Raven.Client.Embedded;
using Tasko.Server.Controllers;
using Tasko.Server.Models;

namespace Tasko.Tests.Integration
{
    public class TasksControllerTests
    {
        [SetUp]
        public void Setup()
        {
            RavenController.Store = new EmbeddableDocumentStore { RunInMemory = true };
            RavenController.Store.Initialize();
        }

        /// <summary>
        /// creates a controller with an open session
        /// </summary>
        public TasksController GetController()
        {
            var c = new TasksController();
            Helper.SetupControllerForTests(c);

            c.RavenSession = RavenController.Store.OpenSession();
            return c;
        }

        /// <summary>
        /// creates a TaskCreateDto
        /// </summary>
        public TaskCreateDto GetDto(string description, string category)
        {
            var dto = new TaskCreateDto();
            dto.Description = description;
            dto.Category = category;
            return dto;
        }

        /// <summary>
        /// returns the content from a HttpResponseMessage
        /// </summary>
        public T GetContentFromResponse<T>(HttpResponseMessage resp)
        {
            return resp.Content.ReadAsAsync<T>().Result;
        }

        [TestFixture]
        public class Post : TasksControllerTests
        {
            [Test]
            public void InsertsOneTask()
            {
                var dto = GetDto("foo", "bar");

                using (var c = GetController())
                {
                    c.Post(dto);
                }

                using (var c = GetController())
                {
                    var resp = c.CreateSearch(new CreateSearchDto());
                    var tasks = this.GetContentFromResponse<IEnumerable<Task>>(resp);

                    Assert.That(tasks.Count() == 1);

                    var task = tasks.First();
                    
                    Assert.AreEqual("foo", task.Description);
                    Assert.AreEqual("bar", task.Categories.First());
                }
            }

            [Test]
            public void ReturnsTaskWithId()
            {
                var dto = GetDto("foo", "bar");

                using (var c = GetController())
                {
                    var resp = c.Post(dto);
                    var task = GetContentFromResponse<Task>(resp);
                    Assert.Greater(task.Id, 0);
                    Assert.AreEqual("foo", task.Description);
                    Assert.AreEqual("bar", task.Categories.First());
                }
            }

            [Test]
            public void ReturnsCreatedOnSuccess()
            {
                using (var c = GetController())
                {
                    var dto = GetDto("foo", "bar");
                    var resp = c.Post(dto);

                    Assert.AreEqual(HttpStatusCode.Created, resp.StatusCode);
                }
            }

            [Test]
            public void ReturnsBadRequestWhenNoDtoIsPassed()
            {
                using (var c = GetController())
                {
                    var resp = c.Post(null);
                    Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
                }
            }
        }

        [TestFixture]
        public class Get : TasksControllerTests
        {
            [Test]
            public void LoadsOneTaskById()
            {
                int taskId = 0;

                using (var c = GetController())
                {
                    var dto = GetDto("foo", "bar");
                    var resp = c.Post(dto);
                    var task = GetContentFromResponse<Task>(resp);
                    taskId = task.Id;
                    Assert.AreNotEqual(0, taskId);
                }

                using (var c = GetController())
                {
                    var task = c.Get(taskId);

                    Assert.IsNotNull(task);
                    Assert.AreEqual("foo", task.Description);
                    Assert.AreEqual("bar", task.Categories.First());
                }
            }

            [Test]
            public void ReturnsNotFoundWhenLoadingNonExistingTaskById()
            {
                using (var c = GetController())
                {
                    var ex = Assert.Throws<HttpResponseException>(() => c.Get(100));
                    Assert.AreEqual(HttpStatusCode.NotFound, ex.Response.StatusCode);
                }
            }

            [Test]
            public void LoadsAllTasks()
            {
                using (var c = GetController())
                {
                    var dto = GetDto("foo1", "bar");
                    c.Post(dto);
                    dto = GetDto("foo2", "bar");
                    c.Post(dto);
                    dto = GetDto("foo3", "bar");
                    c.Post(dto);
                }

                using (var c = GetController())
                {
                    var resp = c.CreateSearch(new CreateSearchDto());
                    var tasks = this.GetContentFromResponse<IEnumerable<Task>>(resp);
                    Assert.IsNotEmpty(tasks);
                    Assert.AreEqual(3, tasks.Count());
                }
            }
        }

        [TestFixture]
        public class CreateSearch : TasksControllerTests
        {
            [Test]
            public void LoadTasksByCategory()
            {
                using (var c = GetController())
                {
                    var dto = GetDto("foo1", "cat1");
                    c.Post(dto);
                    dto = GetDto("foo2", "cat1");
                    c.Post(dto);
                    dto = GetDto("foo3", "cat2");
                    c.Post(dto);
                }

                using (var c = GetController())
                {
                    var resp = c.CreateSearch(new CreateSearchDto { Category = "cat1" });
                    var tasks = this.GetContentFromResponse<IEnumerable<Task>>(resp);

                    Assert.IsNotEmpty(tasks);
                    Assert.AreEqual(2, tasks.Count());
                }
            }

            [Test]
            public void LoadingNonExistingCategoryReturnsEmptyIEnumerable()
            {
                using (var c = GetController())
                {
                    var dto = GetDto("foo", "cat1");
                    c.Post(dto);

                    var resp = c.CreateSearch(new CreateSearchDto { Category = "cat2" });
                    var tasks = this.GetContentFromResponse<IEnumerable<Task>>(resp);
                    Assert.IsEmpty(tasks);
                }
            }

            [Test]
            public void LoadsFinishedAndUnfinishedTasks()
            {
                using (var c = GetController())
                {
                    var dto = GetDto("foo1", "cat1");
                    var task = GetContentFromResponse<Task>(c.Post(dto));
                    int taskid1 = task.Id;

                    dto = GetDto("foo2", "cat1");
                    task = GetContentFromResponse<Task>(c.Post(dto));
                    int taskid2 = task.Id;

                    dto = GetDto("foo3", "cat1");
                    c.Post(dto);

                    c.FinishTask(taskid1);
                    c.FinishTask(taskid2);
                }

                using (var c = GetController())
                {
                    var resp = c.CreateSearch(new CreateSearchDto { Finished = true });
                    var finishedTasks = this.GetContentFromResponse<IEnumerable<Task>>(resp);
                    Assert.IsNotEmpty(finishedTasks);
                    Assert.AreEqual(2, finishedTasks.Count());

                    resp = c.CreateSearch(new CreateSearchDto { Finished = false });
                    var unfinishedTasks = this.GetContentFromResponse<IEnumerable<Task>>(resp);
                    Assert.IsNotEmpty(unfinishedTasks);
                    Assert.AreEqual(1, unfinishedTasks.Count());
                }
            }

            [Test]
            public void LoadsFinishedAndUnfinishedTasksWithCategories()
            {
                int taskid1;
                int taskid2;
                int taskid3;

                using (var c = GetController())
                {
                    var dto = GetDto("foo1", "cat1");
                    var task = GetContentFromResponse<Task>(c.Post(dto));
                    taskid1 = task.Id;

                    dto = GetDto("foo2", "cat2");
                    task = GetContentFromResponse<Task>(c.Post(dto));
                    taskid2 = task.Id;

                    dto = GetDto("foo3", "cat2");
                    task = GetContentFromResponse<Task>(c.Post(dto));
                    taskid3 = task.Id;

                    c.FinishTask(taskid1);
                    c.FinishTask(taskid2);
                }

                using (var c = GetController())
                {
                    var resp = c.CreateSearch(new CreateSearchDto { Category="cat1", Finished = true });
                    var cat1Finished = this.GetContentFromResponse<IEnumerable<Task>>(resp);
                    // should find taskid1
                    Assert.IsNotEmpty(cat1Finished);
                    Assert.AreEqual(1, cat1Finished.Count());
                    Assert.AreEqual(taskid1, cat1Finished.First().Id);

                    resp = c.CreateSearch(new CreateSearchDto { Category = "cat1", Finished = false });
                    var cat1Unfinished = this.GetContentFromResponse<IEnumerable<Task>>(resp);
                    // should find nothing
                    Assert.IsEmpty(cat1Unfinished);
                    Assert.AreEqual(0, cat1Unfinished.Count());

                    resp = c.CreateSearch(new CreateSearchDto { Category = "cat2", Finished = true });
                    var cat2Finished = this.GetContentFromResponse<IEnumerable<Task>>(resp);
                    // should find taskid2
                    Assert.IsNotEmpty(cat2Finished);
                    Assert.AreEqual(1, cat2Finished.Count());
                    Assert.AreEqual(taskid2, cat2Finished.First().Id);

                    resp = c.CreateSearch(new CreateSearchDto { Category = "cat2", Finished = false });
                    var cat2Unfinished = this.GetContentFromResponse<IEnumerable<Task>>(resp);
                    // should find taskid3
                    Assert.IsNotEmpty(cat2Unfinished);
                    Assert.AreEqual(1, cat2Unfinished.Count());
                    Assert.AreEqual(taskid3, cat2Unfinished.First().Id);
                }
            }

            [Test]
            public void PagingWorks()
            {
                int taskid1;
                int taskid2;
                int taskid3;

                using (var c = GetController())
                {
                    var dto = GetDto("foo1", "cat1");
                    var task = GetContentFromResponse<Task>(c.Post(dto));
                    taskid1 = task.Id;

                    dto = GetDto("foo2", "cat2");
                    task = GetContentFromResponse<Task>(c.Post(dto));
                    taskid2 = task.Id;

                    dto = GetDto("foo3", "cat2");
                    task = GetContentFromResponse<Task>(c.Post(dto));
                    taskid3 = task.Id;
                }

                using (var c = GetController())
                {
                    // page 1 - should contain foo1 and foo2
                    var resp = c.CreateSearch(new CreateSearchDto { PageNumber = 1, PageSize = 2 });
                    var results = this.GetContentFromResponse<IEnumerable<Task>>(resp).ToList();

                    Assert.IsNotEmpty(results);
                    Assert.AreEqual(2, results.Count(), "page 1 count");
                    Assert.AreEqual(taskid1, ((Task)results[0]).Id, "task 1");
                    Assert.AreEqual(taskid2, ((Task)results[1]).Id, "task 2");

                    // page 2 - should contain foo3
                    resp = c.CreateSearch(new CreateSearchDto { PageNumber = 2, PageSize = 2 });
                    results = this.GetContentFromResponse<IEnumerable<Task>>(resp).ToList();

                    Assert.IsNotEmpty(results);
                    Assert.AreEqual(1, results.Count(), "page 2 count");
                    Assert.AreEqual(taskid3, ((Task)results[0]).Id, "task 3");
                }
            }
        }

        [TestFixture]
        public class FinishAndReopenTask : TasksControllerTests
        {
            [Test]
            public void TaskIsFinishedAndReopened()
            {
                int taskid = 0;

                using (var c = GetController())
                {
                    var dto = GetDto("foo", "bar");
                    var resp = c.Post(dto);
                    var task = GetContentFromResponse<Task>(resp);
                    taskid = task.Id;
                    Assert.AreNotEqual(0, taskid);
                }

                using (var c = GetController())
                {
                    c.FinishTask(taskid);
                }

                // task should be finished now
                using (var c = GetController())
                {
                    var task = c.Get(taskid);
                    Assert.True(task.IsFinished);

                    c.ReopenTask(taskid);
                }

                // task shouldn't be finished now
                using (var c = GetController())
                {
                    var task = c.Get(taskid);
                    Assert.False(task.IsFinished);
                }
            }
        }

        [TestFixture]
        public class ChangeDescription : TasksControllerTests
        {
            [Test]
            public void DescriptionIsChanged()
            {
                int taskid;

                using (var c = GetController())
                {
                    var resp = c.Post(GetDto("foo", "bar"));
                    var task = GetContentFromResponse<Task>(resp);
                    taskid = task.Id;

                    c.ChangeDescription(taskid, new ChangeDescriptionDto { Description = "test" });
                }

                using (var c = GetController())
                {
                    var task = c.Get(taskid);
                    Assert.AreEqual("test", task.Description);
                }

            }

            [Test]
            public void ReturnsBadRequestWhenDtoIsNull()
            {
                using (var c = GetController())
                {
                    var resp = c.Post(GetDto("foo", "bar"));
                    var task = GetContentFromResponse<Task>(resp);

                    var resp2 = c.ChangeDescription(task.Id, null);
                    Assert.AreEqual(HttpStatusCode.BadRequest, resp2.StatusCode);
                }
            }
        }

        [TestFixture]
        public class GetCategories : TasksControllerTests
        {
            [Test]
            public void ReturnsOneCategory()
            {
                using (var c = GetController())
                {
                    var resp = c.Post(GetDto("foo", "bar"));
                    var task = GetContentFromResponse<Task>(resp);

                    var categories = c.GetCategories(task.Id);
                    Assert.AreEqual(1, categories.Count());
                    Assert.Contains("bar",categories.ToList());
                }
            }
        }

        [TestFixture]
        public class PostNewCategory : TasksControllerTests
        {
            [Test]
            public void NewCategoryIsSaved()
            {
                using (var c = GetController())
                {
                    var resp = c.Post(GetDto("foo", "bar"));
                    var task = GetContentFromResponse<Task>(resp);

                    var resp2 = c.PostNewCategory(task.Id, new PostNewCategoryDto { Category = "test" });
                    Assert.AreEqual(HttpStatusCode.Created, resp2.StatusCode);

                    var categories = c.GetCategories(task.Id);
                    Assert.AreEqual(2, categories.Count());
                    Assert.Contains("test", categories.ToList());
                }
            }

            [Test]
            public void ReturnsBadRequestWhenDtoIsNull()
            {
                using (var c = GetController())
                {
                    var resp = c.Post(GetDto("foo", "bar"));
                    var task = GetContentFromResponse<Task>(resp);

                    var resp2 = c.PostNewCategory(task.Id, null);
                    Assert.AreEqual(HttpStatusCode.BadRequest, resp2.StatusCode);
                }
            }
        }

        [TestFixture]
        public class DeleteCategory : TasksControllerTests
        {
            [Test]
            public void CategoryIsDeleted()
            {
                using (var c = GetController())
                {
                    var resp = c.Post(GetDto("foo", "bar"));
                    var task = GetContentFromResponse<Task>(resp);

                    c.PostNewCategory(task.Id, new PostNewCategoryDto { Category = "bar2" });

                    var resp2 = c.DeleteCategory(task.Id, "bar");
                    Assert.AreEqual(HttpStatusCode.OK, resp2.StatusCode);

                    var categories = c.GetCategories(task.Id);
                    Assert.AreEqual(1, categories.Count());
                    Assert.IsFalse(categories.Contains("bar"));
                }
            }
        }
    }
}
