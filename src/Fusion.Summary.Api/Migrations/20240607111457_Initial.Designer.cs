﻿// <auto-generated />
using System;
using Fusion.Summary.Api.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Fusion.Summary.Api.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240607111457_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Fusion.Summary.Api.Database.Entities.DepartmentTable", b =>
                {
                    b.Property<string>("DepartmentSapId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("FullDepartmentName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("ResourceOwnerAzureUniqueId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("DepartmentSapId");

                    b.ToTable("Departments");
                });
#pragma warning restore 612, 618
        }
    }
}