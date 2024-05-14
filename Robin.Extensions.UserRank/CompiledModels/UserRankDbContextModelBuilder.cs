﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Robin.Extensions.UserRank.CompiledModels
{
    public partial class UserRankDbContextModel
    {
        partial void Initialize()
        {
            var record = RecordEntityType.Create(this);

            RecordEntityType.CreateAnnotations(record);

            AddAnnotation("ProductVersion", "8.0.4");
            AddRuntimeAnnotation("Relational:RelationalModel", CreateRelationalModel());
        }

        private IRelationalModel CreateRelationalModel()
        {
            var relationalModel = new RelationalModel(this);

            var record = FindEntityType("Robin.Extensions.UserRank.Record")!;

            var defaultTableMappings = new List<TableMappingBase<ColumnMappingBase>>();
            record.SetRuntimeAnnotation("Relational:DefaultMappings", defaultTableMappings);
            var robinExtensionsUserRankRecordTableBase = new TableBase("Robin.Extensions.UserRank.Record", null, relationalModel);
            var groupIdColumnBase = new ColumnBase<ColumnMappingBase>("GroupId", "INTEGER", robinExtensionsUserRankRecordTableBase);
            robinExtensionsUserRankRecordTableBase.Columns.Add("GroupId", groupIdColumnBase);
            var recordIdColumnBase = new ColumnBase<ColumnMappingBase>("RecordId", "INTEGER", robinExtensionsUserRankRecordTableBase);
            robinExtensionsUserRankRecordTableBase.Columns.Add("RecordId", recordIdColumnBase);
            var userIdColumnBase = new ColumnBase<ColumnMappingBase>("UserId", "INTEGER", robinExtensionsUserRankRecordTableBase);
            robinExtensionsUserRankRecordTableBase.Columns.Add("UserId", userIdColumnBase);
            relationalModel.DefaultTables.Add("Robin.Extensions.UserRank.Record", robinExtensionsUserRankRecordTableBase);
            var robinExtensionsUserRankRecordMappingBase = new TableMappingBase<ColumnMappingBase>(record, robinExtensionsUserRankRecordTableBase, true);
            robinExtensionsUserRankRecordTableBase.AddTypeMapping(robinExtensionsUserRankRecordMappingBase, false);
            defaultTableMappings.Add(robinExtensionsUserRankRecordMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)recordIdColumnBase, record.FindProperty("RecordId")!, robinExtensionsUserRankRecordMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)groupIdColumnBase, record.FindProperty("GroupId")!, robinExtensionsUserRankRecordMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)userIdColumnBase, record.FindProperty("UserId")!, robinExtensionsUserRankRecordMappingBase);

            var tableMappings = new List<TableMapping>();
            record.SetRuntimeAnnotation("Relational:TableMappings", tableMappings);
            var recordsTable = new Table("Records", null, relationalModel);
            var recordIdColumn = new Column("RecordId", "INTEGER", recordsTable);
            recordsTable.Columns.Add("RecordId", recordIdColumn);
            var groupIdColumn = new Column("GroupId", "INTEGER", recordsTable);
            recordsTable.Columns.Add("GroupId", groupIdColumn);
            var userIdColumn = new Column("UserId", "INTEGER", recordsTable);
            recordsTable.Columns.Add("UserId", userIdColumn);
            var pK_Records = new UniqueConstraint("PK_Records", recordsTable, new[] { recordIdColumn });
            recordsTable.PrimaryKey = pK_Records;
            var pK_RecordsUc = RelationalModel.GetKey(this,
                "Robin.Extensions.UserRank.Record",
                new[] { "RecordId" });
            pK_Records.MappedKeys.Add(pK_RecordsUc);
            RelationalModel.GetOrCreateUniqueConstraints(pK_RecordsUc).Add(pK_Records);
            recordsTable.UniqueConstraints.Add("PK_Records", pK_Records);
            relationalModel.Tables.Add(("Records", null), recordsTable);
            var recordsTableMapping = new TableMapping(record, recordsTable, true);
            recordsTable.AddTypeMapping(recordsTableMapping, false);
            tableMappings.Add(recordsTableMapping);
            RelationalModel.CreateColumnMapping(recordIdColumn, record.FindProperty("RecordId")!, recordsTableMapping);
            RelationalModel.CreateColumnMapping(groupIdColumn, record.FindProperty("GroupId")!, recordsTableMapping);
            RelationalModel.CreateColumnMapping(userIdColumn, record.FindProperty("UserId")!, recordsTableMapping);
            return relationalModel.MakeReadOnly();
        }
    }
}
