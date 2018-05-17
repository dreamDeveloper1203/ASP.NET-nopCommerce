﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Forums;

namespace Nop.Data.Mapping.Forums
{
    /// <summary>
    /// Represents a forum post vote mapping configuration
    /// </summary>
    public partial class ForumPostVoteMap : NopEntityTypeConfiguration<ForumPostVote>
    {
        #region Methods

        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ForumPostVote> builder)
        {
            builder.ToTable("Forums_PostVote");
            builder.HasKey(postVote => postVote.Id);

            builder.HasOne(postVote => postVote.ForumPost)
                .WithMany()
                .HasForeignKey(postVote => postVote.ForumPostId)
                .IsRequired();

            //add custom configuration
            this.PostConfigure(builder);
        }

        #endregion
    }
}