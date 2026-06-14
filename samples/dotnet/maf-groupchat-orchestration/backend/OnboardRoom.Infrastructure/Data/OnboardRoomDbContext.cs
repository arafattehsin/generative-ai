// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;
using OnboardRoom.Domain.Entities;

namespace OnboardRoom.Infrastructure.Data;

public sealed class OnboardRoomDbContext(DbContextOptions<OnboardRoomDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowRun> WorkflowRuns => this.Set<WorkflowRun>();

    public DbSet<WorkflowStepRun> WorkflowStepRuns => this.Set<WorkflowStepRun>();

    public DbSet<GroupChatMessage> GroupChatMessages => this.Set<GroupChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowRun>(entity =>
        {
            entity.ToTable("WorkflowRuns");
            entity.HasKey(run => run.Id);
            entity.Property(run => run.Id).ValueGeneratedNever();
            entity.Property(run => run.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(run => run.Region).HasMaxLength(20);
            entity.Property(run => run.Urgency).HasMaxLength(30);
            entity.Property(run => run.Tone).HasMaxLength(40);
            entity.Property(run => run.InputTextOriginal).HasColumnType("TEXT");
            entity.Property(run => run.InputTextRedacted).HasColumnType("TEXT");
            entity.Property(run => run.ProfileJson).HasColumnType("TEXT");
            entity.Property(run => run.ChairRecommendationJson).HasColumnType("TEXT");
            entity.Property(run => run.FinalOutputHtml).HasColumnType("TEXT");
            entity.Property(run => run.Error).HasMaxLength(4000);
            entity.Property(run => run.RerunFromStep).HasMaxLength(80);
            entity.HasIndex(run => run.CreatedAt);
            entity.HasIndex(run => run.Status);
            entity.HasIndex(run => run.RootRunId);
        });

        modelBuilder.Entity<WorkflowStepRun>(entity =>
        {
            entity.ToTable("WorkflowStepRuns");
            entity.HasKey(step => step.Id);
            entity.Property(step => step.Id).ValueGeneratedNever();
            entity.Property(step => step.StepName).HasMaxLength(80);
            entity.Property(step => step.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(step => step.InputSnapshot).HasColumnType("TEXT");
            entity.Property(step => step.OutputSnapshot).HasColumnType("TEXT");
            entity.Property(step => step.Error).HasMaxLength(4000);
            entity.HasOne(step => step.Run).WithMany(run => run.Steps).HasForeignKey(step => step.RunId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(step => new { step.RunId, step.StepOrder });
            entity.HasIndex(step => new { step.RunId, step.StepName }).IsUnique();
        });

        modelBuilder.Entity<GroupChatMessage>(entity =>
        {
            entity.ToTable("GroupChatMessages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).ValueGeneratedNever();
            entity.Property(message => message.Speaker).HasMaxLength(120);
            entity.Property(message => message.Role).HasMaxLength(80);
            entity.Property(message => message.Content).HasColumnType("TEXT");
            entity.HasOne(message => message.Run).WithMany(run => run.Messages).HasForeignKey(message => message.RunId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(message => new { message.RunId, message.Sequence }).IsUnique();
        });
    }
}
