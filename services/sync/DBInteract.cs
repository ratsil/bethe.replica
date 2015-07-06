using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using helpers;
using helpers.extensions;
using helpers.replica.mam;
using helpers.replica.pl;
using helpers.replica.media;

namespace replica.sync
{
	class DBInteract : helpers.replica.DBInteract
	{
		//struct COMMAND_FILE_UPLOAD_INFO
		//{
		//    public int nCommandUploadID;
		//    public bool bForce;
		//    public COMMAND_FILE_UPLOAD_INFO(int nCommandUploadID, bool bForce)
		//    {
		//        this.nCommandUploadID = nCommandUploadID;
		//        this.bForce = bForce;
		//    }
		//}
		public DBInteract()
		{
			_cDB = new DB();
			_cDB.CredentialsLoad();
		}
		public Dictionary<long, string> ComingUpFilesGet(int nOffset, int nQty)
		{
			Dictionary<long, string> aRetVal = new Dictionary<long, string>();
			try
			{
				Queue<Hashtable> aqDBValues = _cDB.Select("SELECT id, `sPath`, `sFilename` FROM pl.`vComingUp` ORDER BY `dtStartReal`,`dtStartQueued`,`dtStartPlanned` OFFSET " + nOffset + " LIMIT " + nQty);
				Hashtable ahRow = null;
				string sFile;
				while (null != aqDBValues && 0 < aqDBValues.Count)
				{
					ahRow = aqDBValues.Dequeue();
					try
					{
						sFile = ahRow["sPath"].ToString() + "/" + ahRow["sFilename"].ToString();
						sFile = System.IO.Path.GetDirectoryName(sFile) + "/" + System.IO.Path.GetFileName(sFile);
						aRetVal.Add(ahRow["id"].ToID(), sFile);
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex); //TODO LANG
					}
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(new Exception("Ошибка получения списка файлов плейлиста (" + ex.Message + ")")); //TODO LANG
			}
			return aRetVal;
		}
		public void ProcessCommands()
		{
			//try
			//{
			//    int nCommandQueueID = -1, nCommandStatusID = 3;
			//    string sCommandName = null;
			//    Dictionary<string, COMMAND_FILE_UPLOAD_INFO> ahFiles = new Dictionary<string, COMMAND_FILE_UPLOAD_INFO>();
			//    try
			//    {
			//        Queue<Hashtable> aqParams = null, aqDBValues = _cDB.Select("SELECT id, `sCommandName` FROM adm.`vCommandsQueue` WHERE `sCommandName` IN ('file_upload') AND 'waiting'=`sCommandStatus` ORDER BY dt"); //UNDONE сделать нормальную обработку символьных имен команд через Preferences
			//        Hashtable ahRow = null, ahParam = null;
			//        string sFile = null;
			//        bool bForce = false;
			//        while (0 < aqDBValues.Count)
			//        {
			//            try
			//            {
			//                ahRow = aqDBValues.Dequeue();
			//                sCommandName = ahRow["sCommandName"].ToString();
			//                nCommandQueueID = ahRow["id"].ToID();
			//                (new Logger()).WriteNotice("начало выполнения команды [" + sCommandName + "][" + nCommandQueueID + "]");
			//                _cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=2 WHERE id=" + nCommandQueueID);
			//                switch (sCommandName)
			//                {
			//                    case "file_upload":
			//                        sFile = null;
			//                        bForce = false;
			//                        aqParams = _cDB.Select("SELECT `sKey`,`sValue` FROM adm.`tCommandParameters` WHERE `idCommandsQueue`=" + nCommandQueueID); //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
			//                        while (0 < aqParams.Count)
			//                        {
			//                            ahParam = aqParams.Dequeue();
			//                            switch (ahParam["sKey"].ToString())
			//                            {
			//                                case "sFile":
			//                                    sFile = ahParam["sValue"].ToString();
			//                                    break;
			//                                case "bForce":
			//                                    bForce = ahParam["sValue"].ToBool();
			//                                    break;
			//                            }
			//                        }
			//                        if (ahFiles.ContainsKey(sFile))
			//                        {
			//                            //_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=1 WHERE id=" + nCommandQueueID);
			//                            continue;
			//                        }
			//                        ahFiles.Add(sFile, new COMMAND_FILE_UPLOAD_INFO(nCommandQueueID, bForce));
			//                        break;
			//                    case "file_delete":
			//							System.IO.File.Move(ahFiles[nID].sFile, "." + ahFiles[nID].sFile);
			//                        break;
			//                    default:
			//                        throw new Exception("Неизвестная команда");
			//                }
			//            }
			//            catch (Exception ex)
			//            {
			//                (new Logger()).WriteError(ex);
			//            }
			//        }
			//        string sFilename;
			//        File cFile;
			//        long nStorageIDforClips = Storage.Load("Клипы").nID;
			//        long nStorageIDforAdv = Storage.Load("Реклама").nID;
			//        bool bNewFile = false;
			//        foreach (string sUploadFile in ahFiles.Keys)
			//        {
			//            try
			//            {
			//                nCommandStatusID = 3;
			//                sFilename = System.IO.Path.GetFileName(sUploadFile);
			//                cFile = FileGet(sFilename);
			//                if (File.Empty == cFile)
			//                {
			//                    bNewFile = true;
			//                    cFile = FileAdd((System.IO.Path.GetDirectoryName(sUploadFile).ToLower().EndsWith("_muz") ? nStorageIDforClips : nStorageIDforAdv), sFilename);
			//                }
			//                (new Logger()).WriteNotice("Начало обработки файла в рамках команды [cmd:" + ahFiles[sUploadFile].nCommandUploadID + "][frc:" + ahFiles[sUploadFile].bForce + "][beep:" + sUploadFile + "][replica:" + cFile.sFile + "]");//TODO LANG
			//                if (Service.FileDownload(sUploadFile, cFile.sFile, ahFiles[sUploadFile].bForce) || !bNewFile)
			//                {
			//                    _cDB.Perform("INSERT INTO adm.`tCommandParameters` (`idCommandsQueue`,`sKey`,`sValue`) VALUES (" + ahFiles[sUploadFile].nCommandUploadID + ", 'idFiles', '" + cFile.nID + "')");
			//                    nCommandStatusID = 4;
			//                }
			//                else
			//                    (new Logger()).WriteNotice("Ошибка загрузки файла в рамках команды [cmd:" + ahFiles[sUploadFile].nCommandUploadID + "][beep:" + sUploadFile + "][replica:" + cFile.sFile + "]");
			//            }
			//            catch (Exception ex)
			//            {
			//                (new Logger()).WriteError(ex);
			//            }
			//            _cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=" + nCommandStatusID + " WHERE id=" + ahFiles[sUploadFile].nCommandUploadID);
			//            if (4 == nCommandStatusID)
			//                (new Logger()).WriteNotice("Успешное завершение выполнения команды [" + ahFiles[sUploadFile].nCommandUploadID + "]");
			//            else
			//                (new Logger()).WriteNotice("Ошибка выполнения команды [" + ahFiles[sUploadFile].nCommandUploadID + "]");
			//        }
			//        //_cDB.Perform("SELECT pl.`fBeepFileSet`(" + cPLI.nID + ",'" + cPLI.cBeepInfo.sUploadFile + "')");
			//    }
			//    catch (Exception ex)
			//    {
			//        (new Logger()).WriteError(ex);
			//    }
			//}
			//catch (Exception ex)
			//{
			//    (new Logger()).WriteError(ex);
			//}
		}
		public File[] FilesUnusedGet(Storage cStorage)
		{
			List<File> aRetVal = new List<File>();
			try
			{
				Queue<Hashtable> aqDBValues = null;
				if (null != (aqDBValues = _cDB.Select("SELECT * FROM media.`vFilesUnused`" + (null == cStorage ? "" : "WHERE `idStorages` = " + cStorage.nID)))) 
				{
					while (0 < aqDBValues.Count)
						aRetVal.Add(new File(aqDBValues.Dequeue()));
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				return null;
			}
			return aRetVal.ToArray();
		}
		public File[] FilesUnusedGet()
		{
			return FilesUnusedGet(null);
		}
		public void PlaylistItemCached(long nPLIID)
		{
			_cDB.Perform("INSERT INTO pl.`tItemsCached` (`idItems`) VALUES (" + nPLIID + ")");
		}
		public void CacheClear()
		{
			_cDB.Perform("DELETE FROM pl.`tItemsCached` WHERE `idItems` NOT IN (SELECT id FROM pl.`tItems`)");
		}
	}
}
