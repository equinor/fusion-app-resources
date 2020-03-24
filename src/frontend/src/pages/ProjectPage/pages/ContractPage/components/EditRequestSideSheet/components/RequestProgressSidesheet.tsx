import * as React from 'react';
import {
    PersonCard,
} from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import * as styles from '../styles.less';
import { EditRequest } from '..';
import RequestProgressSidesheet, { FailedRequest, SuccessfulRequest } from '../../../../../../../components/RequestProgressSidesheet';

type RequestProgressSidesheetProps = {
    pendingRequests: EditRequest[];
    failedRequests: FailedRequest<EditRequest>[];
    successfulRequests: SuccessfulRequest<EditRequest, PersonnelRequest>[];
    onClose: () => void;
};

type RequestItemProps = {
    request: EditRequest;
};

const PendingRequestProgressItem: React.FC<RequestItemProps> = ({ request }) => {
    return (
        <div className={styles.item}>
            <div className={styles.position}>{request.positionName}</div>
            <div className={styles.person}>
                <PersonCard personId={request.person?.mail} />
            </div>
        </div>
    );
};

const MppRequestProgressSidesheet: React.FC<RequestProgressSidesheetProps> = ({
    pendingRequests,
    failedRequests,
    successfulRequests,
    onClose,
}) => {
    return (
        <RequestProgressSidesheet
            failedRequests={failedRequests}
            pendingRequests={pendingRequests}
            successfulRequests={successfulRequests}
            onClose={onClose}
            renderRequest={PendingRequestProgressItem}
        />
    );
};

export default MppRequestProgressSidesheet;
