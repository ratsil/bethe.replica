//#define SCR

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
			//return; 
			try
			{
				foreach (QueuedCommand cQC in QueuedCommand.Load("`sCommandName` IN ('cues_template_show','cues_template_hide','cues_plugin_playlist_start') AND 'waiting'=`sCommandStatus`", "dt", "1"))
				{
					Template cTemplate = null;
					cQC.StatusChange("proccessing");
					if (cQC.cCommand.sName == "cues_template_show" || cQC.cCommand.sName == "cues_template_hide")
					{
                        QueuedCommand.Parameter cQCP = cQC.aParameters.FirstOrDefault(o => o.sKey == "path");
						if (null != cQCP)
							cTemplate = new Template(cQCP.sValue);
						else
							throw new Exception("There is no 'path' parameter in command [name="+ cQC.cCommand.sName + "]");
					}
					switch (cQC.cCommand.sName)
					{
						case "cues_template_show":
							cTemplate.eCommand = Template.COMMAND.show; // уже давно было не доделано! 
							cTemplate.Prepare();
							break;
						case "cues_template_hide":
							cTemplate.eCommand = Template.COMMAND.hide;  // уже давно было не доделано! 
							break;
						case "cues_plugin_playlist_start":
							cTemplate = new Template(_cDB.GetValue("SELECT `sFile` FROM cues.`tTemplates` WHERE `sName` = 'Экстренный плейлист'"));
							(new Logger("commands")).WriteNotice("template: [file=" + cTemplate.sFile + "][pliid=" + cTemplate.nPlaylistItemID + "][command=" + cTemplate.eCommand + "]");
                            cTemplate.Prepare();
							cTemplate.Start();
							break;
					}
					cQC.StatusChange("succeed");
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
		Queue<PlaylistItem> Cues.IInteract.PlaylistItemsPreparedGet() { return this.PlaylistItemsPreparedAndQueuedGet(); }
        TemplateBind[] Cues.IInteract.TemplateBindsGet(PlaylistItem cPLI) { return this.TemplateBindsActualGet(cPLI); }
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
