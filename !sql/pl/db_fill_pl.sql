﻿/********************************** pl."tStatuses" ****************************************/
SELECT pl."fStatusAdd"('planned');
SELECT pl."fStatusAdd"('queued');
SELECT pl."fStatusAdd"('prepared');
SELECT pl."fStatusAdd"('onair');
SELECT pl."fStatusAdd"('played');
SELECT pl."fStatusAdd"('skipped');
SELECT pl."fStatusAdd"('failed');
/********************************** pl."tClasses" *****************************************/
SELECT pl."fClassAdd"('unknown');
SELECT pl."fClassAdd"('clip_common');
SELECT pl."fClassAdd"('advertisement_common');
SELECT pl."fClassAdd"('design_common');
/********************************** pl."tItems" *******************************************/
/********************************** pl."tItemAttributes" **********************************/
/********************************** pl."tItemDTEventTypes" ********************************/
SELECT pl."fItemDTEventTypeAdd"('start_planned');
SELECT pl."fItemDTEventTypeAdd"('start_hard');
SELECT pl."fItemDTEventTypeAdd"('start_soft');
SELECT pl."fItemDTEventTypeAdd"('timings_update');
SELECT pl."fItemDTEventTypeAdd"('start_queued');
SELECT pl."fItemDTEventTypeAdd"('start_real');
SELECT pl."fItemDTEventTypeAdd"('stop_real');

