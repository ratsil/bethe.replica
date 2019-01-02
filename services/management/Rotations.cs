using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Collections;
using System.IO;
using System.Threading;
using helpers;
using helpers.replica.pl;
using helpers.replica.mam;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Management;

namespace replica.management
{
	class Rotations
	{
		public enum Type
		{
			error = -1,
			first = 1,
			second = 2,
			third = 3,
			fourth = 4,
            foreign = 5
		}
		public enum Sex
		{
			duet = 0,
            mail =1,
			femail=2,
		}
		public enum BlockType
		{
			normal,
			minimum,
			forced,
		}
		private class RArray
		{
			List<Clip> _aRotation, _aSecondary;
			int _ni, _nj, _CurrentIndex;
			DateTime _dtCurrentDate;
			Clip _cClipRetVal;
			bool _bIndexChanged;
			Type _eType;
			public int nTimeMinimalFork;
			public int nTimeForcedFork;
			public TimeSpan tsNormalInterval;
			public TimeSpan tsMinimalInterval;
			public TimeSpan tsForcedInterval;
			public TimeSpan tsArtistInterval;
            public int Count
			{
				get
				{
					if (null != _aRotation)
						return _aRotation.Count;
					else
						return -1;
				}
			}
			public bool AllItemsWasGot
			{
				get
				{
					if (1 > Count || (_CurrentIndex == _ni && _bIndexChanged))
						return true;
					else
						return false;
				}
			}
			public RArray(Type eType, TimeSpan tsNormalInterval, TimeSpan tsMinimalInterval, TimeSpan tsForcedInterval, TimeSpan tsArtistInterval)
			{
				_eType = eType;
				_aRotation = new List<Clip>();
				_aSecondary = new List<Clip>();
				_ni = 0;
				_nj = 0;
				this.tsNormalInterval = tsNormalInterval;
				this.tsMinimalInterval = tsMinimalInterval;
				this.tsForcedInterval = tsForcedInterval;
				this.tsArtistInterval = tsArtistInterval;
				nTimeMinimalFork = tsMinimalInterval == TimeSpan.MinValue ? 0 : (int)tsNormalInterval.Subtract(tsMinimalInterval).TotalMilliseconds;
				nTimeForcedFork = tsForcedInterval == TimeSpan.MinValue ? 0 : (int)tsNormalInterval.Subtract(tsForcedInterval).TotalMilliseconds;
			}
			public void Add(Clip cClip)
			{
				if (null != cClip)
					_aRotation.Add(cClip);
			}
			public void ShuffleRotation()
			{
				List<Clip> aShClips = new List<Clip>();
				int nIndx;
				Random cRND = new Random();
				while (_aRotation.Count > 0)
				{
					nIndx = cRND.Next(0, _aRotation.Count - 1);
					aShClips.Add(_aRotation[nIndx]);
					_aRotation.RemoveAt(nIndx);
				}
				_aRotation = aShClips;
			}
			public Clip GetNext(DateTime dtDate)
			{
				if (dtDate != _dtCurrentDate)
				{
					_CurrentIndex = _ni;
					_bIndexChanged = false;
					_dtCurrentDate = dtDate;
					//if (null != _cLastGotSecondary && _aSecondary.Contains(_cLastGotSecondary))
					//{
					//	_aSecondary.Remove(_cLastGotSecondary);
					//	(new Logger()).WriteDebug2("Services.cs: GetNext: removing from secondary queue ---------- [type=" + this._eType + "] [clip=" + _cLastGotSecondary.sName + "] [secondary.count=" + _aSecondary.Count + "]");
					//}
					//_nj = 0;
				}
				//if (0 < _aSecondary.Count && _nj < _aSecondary.Count)
				//{
				//	return _cLastGotSecondary = _aSecondary[_nj++];
				//}
				//else if (_nj == _aSecondary.Count)  // все уже перебраны и все заблокированы....
				//	_cLastGotSecondary = null;

				if (!AllItemsWasGot)
				{
					_bIndexChanged = true;
					_cClipRetVal = _aRotation[_ni];
					_ni++;
					if (_aRotation.Count == _ni)
						_ni = 0;
					return _cClipRetVal;
				}
				else
					return null;
			}
			public void SetAside(Clip cClip)
			{
				if (!_aSecondary.Contains(cClip))
				{
					_aSecondary.Add(cClip);
					(new Logger()).WriteDebug2("Services.cs: SetAside: [type=" + cClip.cRotation.sName + "] [clip=" + cClip.sName + "] [secondary.count=" + _aSecondary.Count + "]");
				}
			}
		}
		DBInteract _cDBI;
		Dictionary<long, DateTime> _ahArtistsBlocked;
		Dictionary<long, DateTime> _ahClipsBlocked;
		Dictionary<long, DateTime> _ahLastPlayedInArchive;
		Dictionary<Type, RArray> _aAllRotations;
		RArray _aRotation_1, _aRotation_2, _aRotation_3, _aRotation_4;
        Sex _eLastClipsSex;
		Type _eCurrentRotation;
        int _nIndex;
		public Rotations(Queue<Clip> aqClips)
		{
			(new Logger()).WriteNotice("Rotations.cs: constructor in");
			_cDBI = new DBInteract();
			_ahArtistsBlocked = new Dictionary<long, DateTime>();
			_ahClipsBlocked = new Dictionary<long, DateTime>();
			_ahLastPlayedInArchive = _cDBI.ArchiveLastPlayedAssetsGet();
			_aAllRotations = new Dictionary<Type, RArray>();
			_nIndex = 0;
			RotationsSet(aqClips);
			(new Logger()).WriteNotice("Rotations.cs: constructor out");
		}
		private void RotationsSet(Queue<Clip> aqClips)
		{
			Clip cClip;
			List<Clip> aClips = new List<Clip>();
			_aRotation_1 = new RArray(Type.first, (Preferences.tsBlockClip_1_Duration == TimeSpan.MinValue ? Preferences.tsBlockClipDuration : Preferences.tsBlockClip_1_Duration), TimeSpan.MinValue, TimeSpan.MinValue, Preferences.tsBlockArtistDuration);
			_aAllRotations.Add(Type.first, _aRotation_1);
			_aRotation_2 = new RArray(Type.second, (Preferences.tsBlockClip_2_Duration == TimeSpan.MinValue ? Preferences.tsBlockClipDuration : Preferences.tsBlockClip_2_Duration), TimeSpan.MinValue, TimeSpan.MinValue, Preferences.tsBlockArtistDuration);
			_aAllRotations.Add(Type.second, _aRotation_2);
			_aRotation_3 = new RArray(Type.third, (Preferences.tsBlockClip_3_Duration == TimeSpan.MinValue ? Preferences.tsBlockClipDuration : Preferences.tsBlockClip_3_Duration), Preferences.tsBlockClip_3minimum_Duration, Preferences.tsBlockClip_force_Duration, Preferences.tsBlockArtistDuration);
			_aAllRotations.Add(Type.third, _aRotation_3);
			_aRotation_4 = new RArray(Type.fourth, (Preferences.tsBlockClip_4_Duration == TimeSpan.MinValue ? Preferences.tsBlockClipDuration : Preferences.tsBlockClip_4_Duration), Preferences.tsBlockClip_4minimum_Duration, TimeSpan.MinValue, Preferences.tsBlockArtistDuration);
			_aAllRotations.Add(Type.fourth, _aRotation_4);

			while (0 < aqClips.Count)
			{
				cClip = aqClips.Dequeue();
                if (cClip.nFrameOut<25 || cClip.nFramesQty < 25)
                {
                    (new Logger()).WriteError("RotationsSet: found asset with wrong nFrameOut [out=" + cClip.nFrameOut + "][qty=" + cClip.nFramesQty + "]");
                    continue;
                }

				if (null != _ahLastPlayedInArchive && _ahLastPlayedInArchive.ContainsKey(cClip.nID))  //&& cClip.dtLastPlayed == DateTime.MaxValue
					cClip.dtLastPlayed = _ahLastPlayedInArchive[cClip.nID];
				cClip.PersonsLoad();
				cClip.SexLoad();
				aClips.Add(cClip);
            }
			aClips = aClips.OrderBy(o => o.dtLastPlayed).ToList();
			foreach(Clip cCli in aClips)
			{
				BlocksSet(cCli, cCli.dtLastPlayed);
				if (1 > cCli.nFramesQty)
				{
					try
					{
						cCli.nFramesQty = (int)_cDBI.AssetDurationUpdate((Asset)cCli);
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex);
						continue;
					}
				}
				_aAllRotations[ConvertRotationNameToEnum(cCli.cRotation.sName)].Add(cCli);
			}
			foreach (RArray cRA in _aAllRotations.Values)
				cRA.ShuffleRotation();
		}
		private Type ConvertRotationNameToEnum(string sRotationName)
		{
			switch (sRotationName)
			{
				case "первая":
					return Type.first;
				case "вторая":
					return Type.second;
				case "третья":
					return Type.third;
				case "четвёртая":
					return Type.fourth;
				case "иностранная":
					return Type.foreign;
			}
			return Type.third;
		}
		private Sex ConvertSexNameToEnum(string sSexName)
		{
			switch (sSexName)
			{
				case "муж.":
					return Sex.mail;
				case "жен.":
					return Sex.femail;
				case "дуэт":
					return Sex.duet;
			}
			return Sex.duet;
		}
		private Clip GetNext(Type eType, DateTime dtTargetTime, BlockType eBT)
		{
			RArray cRA = _aAllRotations[eType];
			if (eBT == BlockType.minimum && cRA.nTimeMinimalFork == 0 || eBT == BlockType.forced && cRA.nTimeForcedFork == 0)
			{
				(new Logger()).WriteDebug("Rotations.cs: GetNext.before return UPPER [block=" + eBT + "][time=" + dtTargetTime.ToString("yyyy-MM-dd HH:mm:ss") + "]");
				return null;
			}
			if (eBT == BlockType.minimum)
				dtTargetTime = dtTargetTime.AddMilliseconds(1); // чтобы искала заново во всём массиве
			if (eBT == BlockType.forced)
				dtTargetTime = dtTargetTime.AddMilliseconds(2); // чтобы искала заново во всём массиве

			Clip cClip = null;
			int nInd = 0;
			(new Logger()).WriteDebug("Rotations.cs: GetNext.before while [block=" + eBT + "][time=" + dtTargetTime.ToString("yyyy-MM-dd HH:mm:ss") + "]");
			while (null != (cClip = cRA.GetNext(dtTargetTime)))
			{
				nInd++;
				if (nInd % 100 == 0)
					(new Logger()).WriteDebug("Rotations.cs: GetNext [loop=" + nInd + "][time=" + dtTargetTime.ToString("yyyy-MM-dd HH:mm:ss") + "]");

				if (!IsClipBlocked(cClip, dtTargetTime, eBT))
					break;
				//				cRA.SetAside(cClip);
			}
			if (null == cClip && eBT == BlockType.forced)
				throw new Exception("Кончились либо блокированы клипы во всех ротациях со слабым условием блокировки!  [pos=" + _nIndex + "][time=" + dtTargetTime.ToString("yyyy-MM-dd HH:mm:ss") + "]");
			(new Logger()).WriteDebug("Rotations.cs: GetNext.before return [block=" + eBT + "][time=" + dtTargetTime.ToString("yyyy-MM-dd HH:mm:ss") + "][clip=" + (null == cClip ? "null" : cClip.sName) + "]");
			return cClip;
		}
		public Clip GetNextClip(Type eType, DateTime dtTargetTime)
		{
			_nIndex++;
			(new Logger()).WriteNotice("Rotations.cs: GetNextClip in [pos=" + _nIndex + "][time=" + dtTargetTime.ToString("yyyy-MM-dd HH:mm:ss") + "]");
			_eCurrentRotation = eType;
			Clip cRetVal;
			while (true)
			{
				if (eType == Type.error)   
				{
					cRetVal = GetNext(Type.third, dtTargetTime, BlockType.forced);  // по правилу если не нашли в нижних ротациях, то ищем форсированно только в третьей, самой большой...
					break;  // тут не найти не можем - error будет
				}
				else
				{
					cRetVal = GetNext(eType, dtTargetTime, BlockType.normal);
					if (null == cRetVal)
					{
						(new Logger()).WriteNotice("Кончились либо блокированы клипы в ротации (normal): [rot=" + eType + "][original=" + _eCurrentRotation + "][pos=" + _nIndex + "][time=" + dtTargetTime.ToString("yyyy-MM-dd HH:mm:ss") + "]");
						cRetVal = GetNext(eType, dtTargetTime, BlockType.minimum);
					}
					if (null == cRetVal)
					{
						(new Logger()).WriteNotice("Кончились либо блокированы клипы в ротации (minimum): [rot=" + eType + "][original=" + _eCurrentRotation + "][pos=" + _nIndex + "][time=" + dtTargetTime.ToString("yyyy-MM-dd HH:mm:ss") + "]");
						eType = GetLowerType(eType);
					}
					else
						break;
				}
			}
			BlocksSet(cRetVal, dtTargetTime);
			(new Logger()).WriteDebug("отдали: [rot=" + eType + "][original=" + _eCurrentRotation + "][pos=" + _nIndex + "][time=" + dtTargetTime.ToString("yyyy-MM-dd HH:mm:ss") + "]");
			return cRetVal;
		}
		private Type GetLowerType(Type eType)
		{
			Type eTmp = eType;
			if (eType == Type.first) // т.е. или пусто или по кругу уже обошли....
				eType = Type.second;
			if (eType == Type.second)
				eType = Type.third;
			if (eType == Type.third)
				eType = Type.fourth;
			if (eType == Type.fourth)
			{
				//throw new Exception("Кончились, либо блокированы клипы в третьей ротации, чего быть не должно...");
				(new Logger()).WriteWarning("не получилось взять для позиции ["+ _nIndex + "] по правилам ни один клип из ротации " + _eCurrentRotation + " и всех, стоящих ниже неё. Пытаемся взять клип форсированно...");
				return Type.error;
			}
			return eType;
		}
		private void BlocksSet(Clip cCli, DateTime dtTime)
		{
			_eLastClipsSex = null == cCli.cSex ? Sex.duet : ConvertSexNameToEnum(cCli.cSex.sName);

			if (DateTime.MaxValue == dtTime)
			{
				(new Logger()).WriteWarning("очень странно! время выхода рассматриваемого клипа == maxvalue. Нельзя поставить блокировки по времени! [id=" + cCli.nID + "][name=" + cCli.sName + "]");
				return;
			}

			RArray cRA = _aAllRotations[ConvertRotationNameToEnum(cCli.cRotation.sName)];
			DateTime dtClipBlock = dtTime.Add(cRA.tsNormalInterval);
			if (!_ahClipsBlocked.ContainsKey(cCli.nID))
				_ahClipsBlocked.Add(cCli.nID, dtClipBlock);
			else
				_ahClipsBlocked[cCli.nID] = dtClipBlock;

			if (null == cCli.aPersons)
			{
				(new Logger()).WriteWarning("У этого клипа нет артистов! Нельзя поставить блокировки по артистам! [id=" + cCli.nID + "][name=" + cCli.sName + "]");
				return;
			}

			DateTime dtArtistBlock = DateTime.MinValue;
			dtArtistBlock = dtTime.Add(cRA.tsArtistInterval);
			foreach (Person cPers in cCli.aPersons)
			{
				if (!_ahArtistsBlocked.ContainsKey(cPers.nID))
					_ahArtistsBlocked.Add(cPers.nID, dtArtistBlock);
				else
					_ahArtistsBlocked[cPers.nID] = dtArtistBlock;
			}
		}
		private bool IsClipBlocked(Clip cCli, DateTime dtTargetTime, BlockType eBT)
		{
			RArray cRA = _aAllRotations[ConvertRotationNameToEnum(cCli.cRotation.sName)];
			if (eBT != BlockType.forced)
			{
				if (null == cCli.cSex)
					(new Logger()).WriteWarning("У этого клипа нет пола! Нельзя проверить блокировку по полу артиста! [id=" + cCli.nID + "][name=" + cCli.sName + "]");
				else
				{
					if (
							_eLastClipsSex == Sex.mail && ConvertSexNameToEnum(cCli.cSex.sName) == Sex.mail ||
							_eLastClipsSex == Sex.femail && ConvertSexNameToEnum(cCli.cSex.sName) == Sex.femail)
						return true;
				}
			}
			DateTime dtClipTarget = dtTargetTime;
			if (eBT == BlockType.minimum)
				dtClipTarget = dtTargetTime.AddMilliseconds(cRA.nTimeMinimalFork);
			if (eBT == BlockType.forced)
				dtClipTarget = dtTargetTime.AddMilliseconds(cRA.nTimeForcedFork);

			if (_ahClipsBlocked.ContainsKey(cCli.nID) && _ahClipsBlocked[cCli.nID] > dtClipTarget)
				return true;

			if (null != cCli.aPersons)
				foreach (Person cPers in cCli.aPersons)
				{
					if (_ahArtistsBlocked.ContainsKey(cPers.nID) && _ahArtistsBlocked[cPers.nID] > dtTargetTime)
						return true;
				}
			return false;
		}
		public Type CurrentRotationNumberGet(int nTotalIndex)
		{
			int nClearIndex = nTotalIndex - nTotalIndex / 4; // без рекламы
			int nForeign = nClearIndex % 6;
			switch (nClearIndex % 4)
			{
				case 0:
				case 2:
					if (0 == nForeign)
						return Type.foreign;
					else
						return Type.third;
				case 1:
					return Type.first;
				case 3:
					return Type.second;
			}
			return Type.third;
		}
	}
}
