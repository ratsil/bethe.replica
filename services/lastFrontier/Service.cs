using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Collections;
using System.Threading;

using helpers;
using ingenie.userspace;

namespace replica.lfrontier
{
    public partial class Service : ServiceBase
    {
		private ushort _nThreadsFinished;
		private ManualResetEvent _cMREStop;
        
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
			try
			{
				//Thread.Sleep(15000);
				(new Logger()).WriteNotice("������� ������ �� ������");//TODO LANG
				_cMREStop = new ManualResetEvent(false);
				ThreadPool.QueueUserWorkItem(Play, this);

			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
        protected override void OnStop()
        {
			try
			{
				(new Logger()).WriteNotice("������� ������ �� ���������");//TODO LANG
				_cMREStop.Set();
				DateTime dt = DateTime.Now;
				while (1 > _nThreadsFinished && DateTime.Now.Subtract(dt).TotalSeconds < 2)
					Thread.Sleep(300);

			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}

        private void Play(object cStateInfo)
        {
			try
			{
				(new Logger()).WriteNotice("������ \"��������� �����\" �������");//TODO LANG
				Animation cAnimation = new Animation();
				cAnimation.Prepared += new EventDelegate(OnAnimationPrepared);
				cAnimation.Started += new EventDelegate(OnAnimationStarted);
				cAnimation.Stopped += new EventDelegate(OnAnimationStopped);
				cAnimation.bKeepAlive = true;
				cAnimation.cDock = new Dock();
				cAnimation.sFolder = Preferences.sFolder;
				cAnimation.nLoopsQty = 0;
				cAnimation.nLayer = 0;
				cAnimation.Prepare();
				cAnimation.Start();
				_cMREStop.WaitOne();
				cAnimation.Stop();
				cAnimation.Dispose();
            }
            catch (Exception ex)
            {
				(new Logger()).WriteError(ex);
			}
			(new Logger()).WriteNotice("������  \"��������� �����\" ����������");//TODO LANG
			_nThreadsFinished++;
		}

		static void OnAnimationPrepared(Atom cAtom)
		{
			(new Logger()).WriteNotice("������������");
		}
		static void OnAnimationStarted(Atom cAtom)
		{
			(new Logger()).WriteNotice("���������");
		}
		static void OnAnimationStopped(Atom cAtom)
		{
			(new Logger()).WriteNotice("�����������");
		}

		static void cAnimation_EventDone()
		{
		}
    }
}
