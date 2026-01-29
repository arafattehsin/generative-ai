// Copyright (c) Microsoft. All rights reserved.

using Microsoft.EntityFrameworkCore;
using PolicyPackBuilder.Domain.Entities;

namespace PolicyPackBuilder.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for PolicyPack Builder.
/// </summary>
public sealed class PolicyPackDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the PolicyPackDbContext.
    /// </summary>
    public PolicyPackDbContext(DbContextOptions<PolicyPackDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the workflow runs.
    /// </summary>
    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();

    /// <summary>
    /// Gets or sets the workflow step runs.
    /// </summary>
    public DbSet<WorkflowStepRun> WorkflowStepRuns => Set<WorkflowStepRun>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // WorkflowRun configuration
        modelBuilder.Entity<WorkflowRun>(entity =>
        {
            entity.ToTable("WorkflowRuns");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.OptionsJson).HasMaxLength(4000);
            entity.Property(e => e.RerunFromStep).HasMaxLength(100);
            entity.Property(e => e.Error).HasMaxLength(4000);

            // Large text fields stored as TEXT (no length limit in SQLite)
            entity.Property(e => e.InputTextOriginal).HasColumnType("TEXT");
            entity.Property(e => e.InputTextRedacted).HasColumnType("TEXT");
            entity.Property(e => e.FinalOutputHtml).HasColumnType("TEXT");

            // Indexes
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ParentRunId);
            entity.HasIndex(e => e.RootRunId);
        });

        // WorkflowStepRun configuration
        modelBuilder.Entity<WorkflowStepRun>(entity =>
        {
            entity.ToTable("WorkflowStepRuns");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.StepName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Error).HasMaxLength(4000);
            entity.Property(e => e.WarningsJson).HasMaxLength(8000);

            // Large text fields for snapshots
            entity.Property(e => e.InputSnapshot).HasColumnType("TEXT");
            entity.Property(e => e.OutputSnapshot).HasColumnType("TEXT");

            // Foreign key - use the Run navigation property
            entity.HasOne(e => e.Run)
                .WithMany(r => r.Steps)
                .HasForeignKey(e => e.RunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.RunId);
            entity.HasIndex(e => new { e.RunId, e.StepName }).IsUnique();
            entity.HasIndex(e => new { e.RunId, e.StepOrder });
        });
    }
}
