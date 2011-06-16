﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Nop.Admin.Models;
using Nop.Admin.Models.Directory;
using Nop.Admin.Models.News;
using Nop.Admin.Models.Polls;
using Nop.Core;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Polls;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.News;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Telerik.Web.Mvc;
using Telerik.Web.Mvc.UI;
using Nop.Services.Polls;

namespace Nop.Admin.Controllers
{
	[AdminAuthorize]
    public class PollController : BaseNopController
	{
		#region Fields

        private readonly IPollService _pollService;
        private readonly ILanguageService _languageService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizationService _localizationService; 


		#endregion

		#region Constructors

        public PollController(IPollService pollService, ILanguageService languageService,
            IDateTimeHelper dateTimeHelper, ILocalizationService localizationService)
        {
            this._pollService = pollService;
            this._languageService = languageService;
            this._dateTimeHelper = dateTimeHelper;
            this._localizationService = localizationService;
		}

		#endregion 
        
        #region Polls

        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            var polls = _pollService.GetPolls(0, false, 0, 10, true);
            var gridModel = new GridModel<PollModel>
            {
                Data = polls.Select(x =>
                {
                    var m = x.ToModel();
                    if (x.StartDateUtc.HasValue)
                        m.StartDate = _dateTimeHelper.ConvertToUserTime(x.StartDateUtc.Value, DateTimeKind.Utc);
                    if (x.EndDateUtc.HasValue)
                        m.EndDate = _dateTimeHelper.ConvertToUserTime(x.EndDateUtc.Value, DateTimeKind.Utc);
                    m.LanguageName = x.Language.Name;
                    return m;
                }),
                Total = polls.TotalCount
            };
            return View(gridModel);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult List(GridCommand command)
        {
            var polls = _pollService.GetPolls(0, false, command.Page - 1, command.PageSize, true);
            var gridModel = new GridModel<PollModel>
            {
                Data = polls.Select(x =>
                {
                    var m = x.ToModel();
                    if (x.StartDateUtc.HasValue)
                        m.StartDate = _dateTimeHelper.ConvertToUserTime(x.StartDateUtc.Value, DateTimeKind.Utc);
                    if (x.EndDateUtc.HasValue)
                        m.EndDate = _dateTimeHelper.ConvertToUserTime(x.EndDateUtc.Value, DateTimeKind.Utc);
                    m.LanguageName = x.Language.Name;
                    return m;
                }),
                Total = polls.TotalCount
            };
            return new JsonResult
            {
                Data = gridModel
            };
        }

        public ActionResult Create()
        {
            ViewBag.AllLanguages = _languageService.GetAllLanguages(true);
            var model = new PollModel();
            //default values
            model.Published = true;
            model.ShowOnHomePage = true;
            return View(model);
        }

        [HttpPost, FormValueExists("save", "save-continue", "continueEditing")]
        public ActionResult Create(PollModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var poll = model.ToEntity();
                poll.StartDateUtc = model.StartDate;
                poll.EndDateUtc = model.EndDate;
                _pollService.InsertPoll(poll);

                SuccessNotification(_localizationService.GetResource("Admin.ContentManagement.Polls.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = poll.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            ViewBag.AllLanguages = _languageService.GetAllLanguages(true);
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var poll = _pollService.GetPollById(id);
            if (poll == null)
                throw new ArgumentException("No poll found with the specified id", "id");

            ViewBag.AllLanguages = _languageService.GetAllLanguages(true);
            var model = poll.ToModel();
            model.StartDate = poll.StartDateUtc;
            model.EndDate = poll.EndDateUtc;
            return View(model);
        }

        [HttpPost, FormValueExists("save", "save-continue", "continueEditing")]
        public ActionResult Edit(PollModel model, bool continueEditing)
        {
            var poll = _pollService.GetPollById(model.Id);
            if (poll == null)
                throw new ArgumentException("No poll found with the specified id");

            if (ModelState.IsValid)
            {
                poll = model.ToEntity(poll);
                poll.StartDateUtc = model.StartDate;
                poll.EndDateUtc = model.EndDate;
                _pollService.UpdatePoll(poll);

                SuccessNotification(_localizationService.GetResource("Admin.ContentManagement.Polls.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = poll.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            ViewBag.AllLanguages = _languageService.GetAllLanguages(true);
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var poll = _pollService.GetPollById(id);
            if (poll == null)
                throw new ArgumentException("No poll found with the specified id", "id");
            
            _pollService.DeletePoll(poll);

            SuccessNotification(_localizationService.GetResource("Admin.ContentManagement.Polls.Deleted"));
            return RedirectToAction("List");
        }

        #endregion

        #region Poll answer

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult PollAnswers(int pollId, GridCommand command)
        {
            var poll = _pollService.GetPollById(pollId);
            if (poll == null)
                throw new ArgumentException("No poll found with the specified id", "pollId");

            var answers = poll.PollAnswers.OrderBy(x=>x.DisplayOrder).ToList();

            var model = new GridModel<PollAnswerModel>
            {
                Data = answers.Select(x => 
                {
                    return new PollAnswerModel()
                    {
                        Id = x.Id,
                        PollId = x.PollId,
                        Name = x.Name,
                        NumberOfVotes = x.NumberOfVotes,
                        DisplayOrder1 = x.DisplayOrder
                    };
                }),
                Total = answers.Count
            };
            return new JsonResult
            {
                Data = model
            };
        }


        [GridAction(EnableCustomBinding = true)]
        public ActionResult PollAnswerUpdate(PollAnswerModel model, GridCommand command)
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult { Data = "error" };
            }

            var pollAnswer = _pollService.GetPollAnswerById(model.Id);
            pollAnswer.Name = model.Name;
            pollAnswer.DisplayOrder = model.DisplayOrder1;
            _pollService.UpdatePoll(pollAnswer.Poll);

            return PollAnswers(pollAnswer.PollId, command);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult PollAnswerAdd(int pollId, PollAnswerModel model, GridCommand command)
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult { Data = "error" };
            }

            var poll = _pollService.GetPollById(pollId);
            if (poll == null)
                throw new ArgumentException("No poll found with the specified id", "pollId");

            poll.PollAnswers.Add(new PollAnswer 
            {
                Name = model.Name,
                DisplayOrder = model.DisplayOrder1
            });
            _pollService.UpdatePoll(poll);

            return PollAnswers(poll.Id, command);
        }


        [GridAction(EnableCustomBinding = true)]
        public ActionResult PollAnswerDelete(int id, GridCommand command)
        {
            var pollAnswer = _pollService.GetPollAnswerById(id);

            int pollId = pollAnswer.PollId;
            _pollService.DeletePollAnswer(pollAnswer);


            return PollAnswers(pollId, command);
        }

        #endregion
    }
}
