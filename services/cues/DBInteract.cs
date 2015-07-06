//#define SCR

using System;
using System.Collections.Generic;
using System.Text;

using System.Collections;
using System.Collections.Specialized;
using helpers;
using helpers.extensions;
using helpers.replica.mam;
using helpers.replica.pl;
using helpers.replica.media;
using helpers.replica.cues;
using helpers.replica.adm;
using ingenie.userspace;

namespace replica.cues
{
	public class DBInteract : helpers.replica.DBInteract, Cues.IInteract, Template.IInteract
	{
		public DBInteract()
		{
			Load();
		}
		public void ProcessCommands()
		{
			return;
			try
			{
				//UNDONE нужно сделать что-то типа QueuedCommandsGet в Helper.replica.DBInteract
				Queue<Hashtable> aqDBValues = _cDB.Select("SELECT * FROM adm.`vCommandsQueue` WHERE `sCommandName` IN ('cues_template_show','cues_template_hide') AND 'waiting'=`sCommandStatus` ORDER BY dt LIMIT 1");
				if (null != aqDBValues && 0 < aqDBValues.Count)
				{
					QueuedCommand cCommand = new QueuedCommand(aqDBValues.Dequeue());
					_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=4 WHERE id=" + cCommand.nID);//UNDONE нужны соответствующие функции в БД, в helpers.replica.DBInteract и в helpers.replica.QueuedCommand
					string sTemplateFile = _cDB.GetValue("SELECT `sValue` FROM adm.`tCommandParameters` WHERE 'path'=`sKey` AND `idCommandsQueue`=" + cCommand.nID);//UNDONE нужны соответствующие функции в БД, в helpers.replica.DBInteract и в helpers.replica.QueuedCommand
					Template cTemplate = new Template(sTemplateFile);
					cTemplate.cTag = cCommand;
					//cTemplate.nTemplateID = cTemplate.sFile.GetHashCode();
					//cTemplate.nPLIID = cues.cCurrentPLIID;
					switch (cCommand.cCommand.sName)
					{
						case "cues_template_show":
							cTemplate.eCommand = Template.COMMAND.show;
							cTemplate.Prepare();
							break;
						case "cues_template_hide":
							cTemplate.eCommand = Template.COMMAND.hide;
							break;
					}
					//cCues.ProcessTemplate(cTemplate);
				}
			}
			catch (Exception ex)
			{
				(new Logger("commands")).WriteError(ex);
			}
		}

		#region реализация Cues.IInteract
		Cues.IInteract Cues.IInteract.Init()
		{
			return new DBInteract();
		}

		PlaylistItem Cues.IInteract.PlaylistItemOnAirGet() { return this.PlaylistItemOnAirGet(); }
		Queue<PlaylistItem> Cues.IInteract.PlaylistItemsPreparedGet() { return this.PlaylistItemsQueuedGet(); }
        TemplateBind[] Cues.IInteract.TemplateBindsGet(PlaylistItem cPLI) { return this.TemplateBindsGet(cPLI); }
        #endregion реализация Cues.IInteract
		#region реализация Template.IInteract
		Template.IInteract Template.IInteract.Init()
		{
			return new DBInteract();
		}

		Macro Template.IInteract.MacroGet(string sMacroName) { return Macro.Get(sMacroName); }
		string Template.IInteract.MacroExecute(Macro cMacro) { return cMacro.Execute(); }
        TemplatesSchedule[] Template.IInteract.TemplatesScheduleGet() { return this.TemplatesScheduleGet(); }
		void Template.IInteract.TemplatesScheduleSave(TemplatesSchedule cTemplatesSchedule) { this.TemplatesScheduleSave(cTemplatesSchedule); }
		void Template.IInteract.TemplateStarted(Template.Range cRange) { cRange.cTemplateBind.cTemplate.Started(cRange.cPlaylistItem); }
		PlaylistItem Template.IInteract.PlaylistItemPreviousGet(PlaylistItem cPLI) { return this.PlaylistItemPreviousGet(cPLI); }
		#endregion реализация Template.IInteract
	}
}
