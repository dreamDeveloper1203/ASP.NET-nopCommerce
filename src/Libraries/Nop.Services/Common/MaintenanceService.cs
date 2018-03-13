using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Infrastructure;
using Nop.Data;

namespace Nop.Services.Common
{
    /// <summary>
    ///  Maintenance service
    /// </summary>
    public partial class MaintenanceService : IMaintenanceService
    {
        #region Fields

        private readonly IDataProvider _dataProvider;
        private readonly IDbContext _dbContext;
        private readonly CommonSettings _commonSettings;
        private readonly INopFileProvider _fileProvider;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="dbContext">Database Context</param>
        /// <param name="commonSettings">Common settings</param>
        /// <param name="fileProvider">File provider</param>
        public MaintenanceService(IDataProvider dataProvider, IDbContext dbContext,
            CommonSettings commonSettings, INopFileProvider fileProvider)
        {
            this._dataProvider = dataProvider;
            this._dbContext = dbContext;
            this._commonSettings = commonSettings;
            this._fileProvider = fileProvider;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get directory path for backs
        /// </summary>
        /// <param name="ensureFolderCreated">A value indicating whether a directory should be created if it doesn't exist</param>
        /// <returns></returns>
        protected virtual string GetBackupDirectoryPath(bool ensureFolderCreated = true)
        {
            var path = _fileProvider.GetAbsolutePath("db_backups\\");
            if (ensureFolderCreated)
                _fileProvider.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Check whether backups are supported
        /// </summary>
        protected virtual void CheckBackupSupported()
        {
            if(_dataProvider.BackupSupported) return;

            throw new DataException("This database does not support backup");
        }
        
        #endregion
        
        #region Methods

        /// <summary>
        /// Get the current ident value
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <returns>Integer ident; null if cannot get the result</returns>
        public virtual int? GetTableIdent<T>() where T: BaseEntity
        {
            //stored procedures aren't supported
            if (!_commonSettings.UseStoredProceduresIfSupported || !_dataProvider.StoredProceduredSupported)
                return null;

            //stored procedures are enabled and supported by the database
            var tableName = _dbContext.GetTableName<T>();
            var result = _dbContext.SqlQuery<decimal?>($"SELECT IDENT_CURRENT('[{tableName}]')").FirstOrDefault();
            return result.HasValue ? Convert.ToInt32(result) : 1;            
        }

        /// <summary>
        /// Set table ident (is supported)
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <param name="ident">Ident value</param>
        public virtual void SetTableIdent<T>(int ident) where T : BaseEntity
        {
            if (_commonSettings.UseStoredProceduresIfSupported && _dataProvider.StoredProceduredSupported)
            {
                var currentIdent = GetTableIdent<T>();
                if (!currentIdent.HasValue || ident <= currentIdent.Value)
                    return;

                //stored procedures are enabled and supported by the database.
                var tableName = _dbContext.GetTableName<T>();
                _dbContext.ExecuteSqlCommand($"DBCC CHECKIDENT([{tableName}], RESEED, {ident})");
            }
            else
            {
                throw new Exception("Stored procedures are not supported by your database");
            }
        }

        /// <summary>
        /// Gets all backup files
        /// </summary>
        /// <returns>Backup file collection</returns>
        public virtual IList<string> GetAllBackupFiles()
        {
            var path = GetBackupDirectoryPath();

            if (!_fileProvider.DirectoryExists(path))
            {
                throw new IOException("Backup directory not exists");
            }
            
            return _fileProvider.GetFiles(path, "*.bak")
                .OrderByDescending(p => _fileProvider.GetLastWriteTime(p)).ToList();
        }

        /// <summary>
        /// Creates a backup of the database
        /// </summary>
        public virtual void BackupDatabase()
        {
            CheckBackupSupported();
            var fileName = $"{GetBackupDirectoryPath()}database_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{CommonHelper.GenerateRandomDigitCode(10)}.bak";

            var commandText = $"BACKUP DATABASE [{_dbContext.DbName()}] TO DISK = '{fileName}' WITH FORMAT";

            _dbContext.ExecuteSqlCommand(commandText, true);
        }

        /// <summary>
        /// Restores the database from a backup
        /// </summary>
        /// <param name="backupFileName">The name of the backup file</param>
        public virtual void RestoreDatabase(string backupFileName)
        {
            CheckBackupSupported();
            var settings = new DataSettingsManager(_fileProvider);
            var conn = new SqlConnectionStringBuilder(settings.LoadSettings().DataConnectionString)
            {
                InitialCatalog = "master"
            };

            //this method (backups) works only with SQL Server database
            using (var sqlConnectiononn = new SqlConnection(conn.ToString()))
            {
                var commandText = string.Format(
                    "DECLARE @ErrorMessage NVARCHAR(4000)\n" +
                    "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE\n" +
                    "BEGIN TRY\n" +
                        "RESTORE DATABASE [{0}] FROM DISK = '{1}' WITH REPLACE\n" +
                    "END TRY\n" +
                    "BEGIN CATCH\n" +
                        "SET @ErrorMessage = ERROR_MESSAGE()\n" +
                    "END CATCH\n" +
                    "ALTER DATABASE [{0}] SET MULTI_USER WITH ROLLBACK IMMEDIATE\n" +
                    "IF (@ErrorMessage is not NULL)\n" +
                    "BEGIN\n" +
                        "RAISERROR (@ErrorMessage, 16, 1)\n" +
                    "END",
                    _dbContext.DbName(),
                    backupFileName);

                DbCommand dbCommand = new SqlCommand(commandText, sqlConnectiononn);
                if (sqlConnectiononn.State != ConnectionState.Open)
                    sqlConnectiononn.Open();
                dbCommand.ExecuteNonQuery();
            }

            //clear all pools
            SqlConnection.ClearAllPools();
        }

        /// <summary>
        /// Returns the path to the backup file
        /// </summary>
        /// <param name="backupFileName">The name of the backup file</param>
        /// <returns>The path to the backup file</returns>
        public virtual string GetBackupPath(string backupFileName)
        {
            return _fileProvider.Combine(GetBackupDirectoryPath(), backupFileName);
        }
        
        #endregion
    }
}
