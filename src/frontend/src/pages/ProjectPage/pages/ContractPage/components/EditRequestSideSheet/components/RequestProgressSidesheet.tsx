
import {
    PersonCard,
} from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import styles from '../styles.less';
import { EditRequest } from '..';
import RequestProgressSidesheet, { FailedRequest, SuccessfulRequest } from '../../../../../../../components/RequestProgressSidesheet';
import { FC } from 'react';

type RequestProgressSidesheetProps = {
    pendingRequests: EditRequest[];
    failedRequests: FailedRequest<EditRequest>[];
    successfulRequests: SuccessfulRequest<EditRequest, PersonnelRequest>[];
    onClose: () => void;
    onRemoveFailedRequest: (request: FailedRequest<EditRequest>) => void;
};

type RequestItemProps = {
    request: EditRequest;
};

const PendingRequestProgressItem: FC<RequestItemProps> = ({ request }) => {
    return (
        <div className={styles.item}>
            <div className={styles.position}>{request.positionName}</div>
            <div className={styles.person}>
                <PersonCard personId={request.person?.mail} />
            </div>
        </div>
    );
};

const MppRequestProgressSidesheet: FC<RequestProgressSidesheetProps> = ({
    pendingRequests,
    failedRequests,
    successfulRequests,
    onClose,
    onRemoveFailedRequest,
}) => {
    return (
        <RequestProgressSidesheet
            title="Saving requests"
            failedRequests={failedRequests}
            pendingRequests={pendingRequests}
            successfulRequests={successfulRequests}
            onClose={onClose}
            renderRequest={PendingRequestProgressItem}
            onRemoveFailedRequest={onRemoveFailedRequest}
        />
    );
};

export default MppRequestProgressSidesheet;
