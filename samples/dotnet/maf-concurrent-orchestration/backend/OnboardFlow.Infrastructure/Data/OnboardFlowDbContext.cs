// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;
using OnboardFlow.Domain.Entities;

namespace OnboardFlow.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for OnboardFlow.
/// </summary>
public sealed class OnboardFlowDbContext : DbContext
{
    public OnboardFlowDbContext(DbContextOptions<OnboardFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();
    public DbSet<WorkflowStepRun> WorkflowStepRuns => Set<WorkflowStepRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkflowRun>(entity =>
        {
            entity.ToTable("WorkflowRuns");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.RerunFromStep).HasMaxLength(100);
            entity.Property(e => e.Error).HasMaxLength(4000);
            entity.Property(e => e.InputTextOriginal).HasColumnType("TEXT");
            entity.Property(e => e.InputTextRedacted).HasColumnType("TEXT");
            entity.Property(e => e.FinalOutputHtml).HasColumnType("TEXT");

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ParentRunId);
            entity.HasIndex(e => e.RootRunId);
        });

        modelBuilder.Entity<WorkflowStepRun>(entity =>
        {
            entity.ToTable("WorkflowStepRuns");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.StepName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Error).HasMaxLength(4000);
            entity.Property(e => e.WarningsJson).HasMaxLength(8000);
            entity.Property(e => e.ConcurrencyGroup).HasMaxLength(100);
            entity.Property(e => e.InputSnapshot).HasColumnType("TEXT");
            entity.Property(e => e.OutputSnapshot).HasColumnType("TEXT");

            entity.HasOne(e => e.Run)
                .WithMany(r => r.Steps)
                .HasForeignKey(e => e.RunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.RunId);
            entity.HasIndex(e => new { e.RunId, e.StepName }).IsUnique();
            entity.HasIndex(e => new { e.RunId, e.StepOrder });
        });
    }
}
