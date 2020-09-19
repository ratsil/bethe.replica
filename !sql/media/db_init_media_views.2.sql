CREATE OR REPLACE VIEW media."vFilesUnused" as
	SELECT * 
		FROM media."vFiles" 
		WHERE id NOT IN 
			(
				SELECT "idFiles" FROM pl."tItems" GROUP BY "idFiles" 
				UNION 
				SELECT "idFiles" FROM mam."vAssetsFiles" GROUP BY "idFiles" 
				UNION 
				SELECT "idFiles" FROM pl."tPlugs" GROUP BY "idFiles"
			);