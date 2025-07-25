﻿// <auto-generated />
using System;
using MedicineTrack.Medication.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedicineTrack.Medication.Migrations.Migrations
{
    [DbContext(typeof(MedicationDbContext))]
    [Migration("20250716114637_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("MedicineTrack.Api.Models.Medication", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("BrandName")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("Color")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateOnly?>("EndDate")
                        .HasColumnType("date");

                    b.Property<string>("Form")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("GenericName")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<bool>("IsArchived")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<string>("Shape")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateOnly>("StartDate")
                        .HasColumnType("date");

                    b.Property<string>("Strength")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.ToTable("Medications");
                });

            modelBuilder.Entity("MedicineTrack.Api.Models.MedicationLog", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("LoggedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("MedicationId")
                        .HasColumnType("uuid");

                    b.Property<string>("Notes")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<double?>("QuantityTaken")
                        .HasColumnType("double precision");

                    b.Property<Guid?>("ScheduleId")
                        .HasColumnType("uuid");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("TakenAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("MedicationId");

                    b.ToTable("MedicationLogs");
                });

            modelBuilder.Entity("MedicineTrack.Api.Models.Medication", b =>
                {
                    b.OwnsMany("MedicineTrack.Api.Models.Schedule", "Schedules", b1 =>
                        {
                            b1.Property<Guid>("MedicationId")
                                .HasColumnType("uuid");

                            b1.Property<Guid>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("uuid");

                            b1.Property<string>("DaysOfWeek")
                                .HasColumnType("text");

                            b1.Property<string>("FrequencyType")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.Property<int?>("Interval")
                                .HasColumnType("integer");

                            b1.Property<double?>("Quantity")
                                .HasColumnType("double precision");

                            b1.Property<string>("TimesOfDay")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.Property<string>("Unit")
                                .HasMaxLength(50)
                                .HasColumnType("character varying(50)");

                            b1.HasKey("MedicationId", "Id");

                            b1.ToTable("Schedules");

                            b1.WithOwner()
                                .HasForeignKey("MedicationId");
                        });

                    b.Navigation("Schedules");
                });

            modelBuilder.Entity("MedicineTrack.Api.Models.MedicationLog", b =>
                {
                    b.HasOne("MedicineTrack.Api.Models.Medication", null)
                        .WithMany()
                        .HasForeignKey("MedicationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
