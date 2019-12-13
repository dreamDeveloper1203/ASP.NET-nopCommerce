﻿using System.Data;
using FluentMigrator;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Polls;
using Nop.Data.Extensions;

namespace Nop.Data.Migrations.Polls
{
    /// <summary>
    /// Represents a poll mapping configuration
    /// </summary>
    [Migration(637097816025962851)]
    public class AddPollLanguageFK : AutoReversingMigration
    {
        #region Methods

        public override void Up()
        {
            this.AddForeignKey(nameof(Poll)
                , nameof(Poll.LanguageId)
                , nameof(Language)
                , nameof(Language.Id)
                , Rule.Cascade);
        }

        #endregion
    }
}