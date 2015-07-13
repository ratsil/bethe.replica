INSERT INTO cues."tPlugins" ("sName") VALUES ('playlist');

INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tPlugins'))."nValue", NULL, false, 'instance');
INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tBinds'))."nValue", (hk."fRegisteredTableGet"('cues', 'tStrings'))."nValue", true, 'name');
INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tBinds'))."nValue", (hk."fRegisteredTableGet"('cues', 'tTimestamps'))."nValue", true, 'start');
INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tBinds'))."nValue", (hk."fRegisteredTableGet"('cues', 'tTimestamps'))."nValue", true, 'stop');
INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tBinds'))."nValue", (hk."fRegisteredTableGet"('mam', 'tAssets'))."nValue", false, 'item');

