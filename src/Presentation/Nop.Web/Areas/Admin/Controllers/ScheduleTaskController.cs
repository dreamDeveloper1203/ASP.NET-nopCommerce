﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Tasks;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Tasks;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.ModelBinding;
using Task = Nop.Services.Tasks.Task;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class ScheduleTaskController : BaseAdminController
    {
        #region Fields

        private readonly ICustomerActivityService _customerActivityService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly IScheduleTaskModelFactory _scheduleTaskModelFactory;
        private readonly IScheduleTaskService _scheduleTaskService;

        #endregion

        #region Ctor

        public ScheduleTaskController(ICustomerActivityService customerActivityService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            IScheduleTaskModelFactory scheduleTaskModelFactory,
            IScheduleTaskService scheduleTaskService)
        {
            _customerActivityService = customerActivityService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _scheduleTaskModelFactory = scheduleTaskModelFactory;
            _scheduleTaskService = scheduleTaskService;
        }

        #endregion

        #region Methods

        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageScheduleTasks))
                return AccessDeniedView();

            //prepare model
            var model = await _scheduleTaskModelFactory.PrepareScheduleTaskSearchModelAsync(new ScheduleTaskSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> List(ScheduleTaskSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageScheduleTasks))
                return AccessDeniedDataTablesJson();

            //prepare model
            var model = await _scheduleTaskModelFactory.PrepareScheduleTaskListModelAsync(searchModel);

            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> TaskUpdate(ScheduleTaskModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageScheduleTasks))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return ErrorJson(ModelState.SerializeErrors());

            //try to get a schedule task with the specified id
            var scheduleTask = await _scheduleTaskService.GetTaskByIdAsync(model.Id)
                               ?? throw new ArgumentException("Schedule task cannot be loaded");

            scheduleTask = model.ToEntity(scheduleTask);

            await _scheduleTaskService.UpdateTaskAsync(scheduleTask);

            //activity log
            await _customerActivityService.InsertActivityAsync("EditTask",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditTask"), scheduleTask.Id), scheduleTask);

            return new NullJsonResult();
        }

        public virtual async Task<IActionResult> RunNow(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageScheduleTasks))
                return AccessDeniedView();

            try
            {
                //try to get a schedule task with the specified id
                var scheduleTask = await _scheduleTaskService.GetTaskByIdAsync(id)
                                   ?? throw new ArgumentException("Schedule task cannot be loaded", nameof(id));

                //ensure that the task is enabled
                var task = new Task(scheduleTask) { Enabled = true };
                await task.Execute(true, false);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.System.ScheduleTasks.RunNow.Done"));
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc);
            }

            return RedirectToAction("List");
        }

        #endregion
    }
}