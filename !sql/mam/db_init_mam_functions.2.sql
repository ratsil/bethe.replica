CREATE OR REPLACE FUNCTION cast_to_xml(oAsset mam."vAssetsResolved") RETURNS xml AS
	$BODY$
	DECLARE
		oRetVal xml;
	BEGIN
		return xmlforest(
				oAsset.id as "nID",
				oAsset."sName",
				xmlforest(
					oAsset."idVideos" as "nID",
					oAsset."sVideoName" as "sName",
					xmlforest(
						oAsset."idVideoTypes" as "nID",
						oAsset."sVideoTypeName" as "sName"
					) as "cType"
				) as "stVideo",
				xmlforest(
					oAsset."idFiles" as "nID",
					oAsset."sFilename",
					xmlforest(
						oAsset."idStorages" as "nID",
						oAsset."sStorageName" as "sName",
						oAsset."sPath" as "sPath",
						oAsset."bStorageEnabled" as "bEnabled",
						xmlforest(
							oAsset."idStorageTypes" as "nID",
							oAsset."sStorageTypeName" as "sName"
						) as "cType"
					) as "cStorage",
					oAsset."dtLastFileEvent" as "dtLastFileEvent",
					oAsset."eFileError" as "eError"
				) as "cFile",
				oAsset."nFrameOut", 
				oAsset."nFramesQty", 
				oAsset."dtLastPlayed", 
				oAsset."bPLEnabled" as "bEnabled", 
				xmlforest(
					oAsset."idClasses" as "nID",
					oAsset."sClassName" as "sName"
				) as "cClass",
				oAsset."idParent" as "nIDParent", 
				oAsset."nFrameIn", 
				xmlforest(
					oAsset."idRotations" as "nID",
					oAsset."sRotationName" as "sName"
				) as "cRotation",
				xmlforest(
					oAsset."idCues" as "nID",
					oAsset."sCueSong" as "sSong",
					oAsset."sCueArtist" as "sArtist",
					oAsset."sCueAlbum" as "sAlbum",
					oAsset."nCueYear" as "sYear",
					oAsset."sCuePossessor" as "sPossessor"
				) as "stCues"
			);
	END;	
	$BODY$
	LANGUAGE plpgsql VOLATILE;
	CREATE CAST (mam."vAssetsResolved" AS xml) WITH FUNCTION cast_to_xml(mam."vAssetsResolved");

CREATE OR REPLACE FUNCTION cast_from_xml_mam_vAssetsResolved(oXML xml) RETURNS mam."vAssetsResolved" AS
	$BODY$
	DECLARE
		sError text;
		oRecord RECORD;
	BEGIN
		sError := (xpath('/*/cFile/eError/text()', oXML))[1]::text;
		IF oXML IS NULL THEN
			RETURN NULL;
		END IF;
		SELECT ROW(
			(xpath('/*/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/sName/text()', oXML))[1]::text,

			(xpath('/*/stVideo/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/stVideo/sName/text()', oXML))[1]::text,
			(xpath('/*/stVideo/cType/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/stVideo/cType/sName/text()', oXML))[1]::text,

			NULL,
			NULL,
			NULL,

			(xpath('/*/cRotation/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/cRotation/sName/text()', oXML))[1]::text,

			(xpath('/*/cPalette/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/cPalette/sName/text()', oXML))[1]::text,

			NULL,
			NULL,
			NULL,
			NULL,

			(xpath('/*/stCues/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/stCues/sSong/text()', oXML))[1]::text,
			(xpath('/*/stCues/sArtist/text()', oXML))[1]::text,
			(xpath('/*/stCues/sAlbum/text()', oXML))[1]::text,
			(xpath('/*/stCues/nYear/text()', oXML))[1]::text::integer,
			(xpath('/*/stCues/sPossessor/text()', oXML))[1]::text,

			(xpath('/*/cFile/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/cFile/cStorage/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/cFile/cStorage/cType/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/cFile/cStorage/cType/sName/text()', oXML))[1]::text,
			(xpath('/*/cFile/cStorage/bEnabled/text()', oXML))[1]::text::boolean,
			(xpath('/*/cFile/cStorage/sPath/text()', oXML))[1]::text,
			(xpath('/*/cFile/cStorage/sName/text()', oXML))[1]::text,
			(xpath('/*/cFile/sFilename/text()', oXML))[1]::text,
			(xpath('/*/cFile/dtLastEvent/text()', oXML))[1]::text::timestamptz,
			NULL,
			CASE WHEN 'no'=sError THEN NULL ELSE sError END,

			(xpath('/*/nFrameIn/text()', oXML))[1]::text::integer,
			(xpath('/*/nFrameOut/text()', oXML))[1]::text::integer,
			(xpath('/*/nFramesQty/text()', oXML))[1]::text::integer,
			(xpath('/*/dtLastPlayed/text()', oXML))[1]::text::timestamptz,
			(xpath('/*/bEnabled/text()', oXML))[1]::text::boolean,

			(xpath('/*/cClass/nID/text()', oXML))[1]::text::integer,
			(xpath('/*/cClass/sName/text()', oXML))[1]::text,
			NULL,
			NULL,
			(xpath('/*/nIDParent/text()', oXML))[1]::text::integer,
			NULL
		)::mam."vAssetsResolved" as oRetVal INTO oRecord;
		RETURN oRecord.oRetVal;
	END
	$BODY$
	LANGUAGE plpgsql VOLATILE;
	CREATE CAST (xml as mam."vAssetsResolved") WITH FUNCTION cast_from_xml_mam_vAssetsResolved(xml);