using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;

using System.Threading;
using System.Collections;
using System.IO;
using System.Xml;
using helpers;
using helpers.replica.pl;
using helpers.replica.cues;

namespace replica.cues
{
	public partial class Service : ServiceBase
	{
		private Cues _cCues;
		private bool _bRunning;
		private int _nThreadsFinished;

		public Service()
		{
			InitializeComponent();
		}
		public void Start()
		{
			OnStart(null);
		}
		protected override void OnStart(string[] args) 
        {
			base.OnStart(args);

			try
			{
				(new Logger("service")).WriteNotice("������� ������ �� ������");//TODO LANG
				_bRunning = true;
				_nThreadsFinished = 0;

				Template.iInteract = new DBInteract();
				_cCues = new Cues();
				_cCues.WatcherCommands = WatcherCommands;
				_cCues.Start(new DBInteract());
			}
			catch (Exception ex)
			{
				(new Logger("service")).WriteError(ex);
			}
		}
        protected override void OnStop()
        {
			try
			{
				(new Logger("service")).WriteNotice("������� ������ �� ���������");//TODO LANG
				_bRunning = false;
				Thread.Sleep(2000);
				Template.ProccesingStop();
			}
			catch (Exception ex)
			{
				(new Logger("service")).WriteError(ex);
			}
		}

		private void WatcherCommands(object cStateInfo)
		{
			(new Logger("commands")).WriteNotice("������ ���������� ��������� �������");//TODO LANG
			while (_bRunning)
			{
				try
				{
					(new DBInteract()).ProcessCommands();
					Thread.Sleep(300);
				}
				catch (Exception ex)
				{
					(new Logger("commands")).WriteError(ex);
					Thread.Sleep(5000);
				}
			}
			(new Logger("commands")).WriteNotice("������ ���������� ��������� ����������");//TODO LANG
			_nThreadsFinished++;
		}
	}
}
