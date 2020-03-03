import * as React from 'react';
import {
    useTooltipRef,
    DoneIcon,
    SyncIcon,
    HistoryIcon,
    CloseIcon,
} from '@equinor/fusion-components';
import classNames from 'classnames';

import * as styles from './styles.less';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';

type RequestStateProps = {
    item: PersonnelRequest;
};

type RequestItemState = 'approved' | 'pending' | 'rejected';

type RequestItemProps = {
    requestStatus: RequestItemState;
    index: number;
};

const states = ['Created state', 'Sumbitted state', 'Approved state'];

const getRequestStates = (request: PersonnelRequest): RequestItemState[] | null => {
    const state = request.state;

    switch (state) {
        case 'Created':
            return ['approved', 'pending', 'pending'];
        case 'Submitted':
            return ['approved', 'approved', 'pending'];
        case 'Approved':
            return ['approved', 'approved', 'rejected'];
        case 'Rejected':
            return ['approved', 'approved', 'approved'];
        default:
            return null;
    }
};

const getRequestIcon = (requestStatus: RequestItemState) => {
    const iconSize = 8;
    switch (requestStatus) {
        case 'approved':
            return <DoneIcon width={iconSize} height={iconSize} />;
        case 'pending':
            return <SyncIcon width={iconSize} height={iconSize} />;
        case 'rejected':
            return <CloseIcon width={iconSize} height={iconSize} />;
        default:
            return <HistoryIcon width={iconSize} height={iconSize} />;
    }
};
const RequestItem: React.FC<RequestItemProps> = ({ requestStatus, index }) => {
    const tooltipRef = useTooltipRef(`${states[index]}: ${requestStatus}`);
    const className = classNames(styles.step, styles[requestStatus]);

    return (
        <div ref={tooltipRef} className={className}>
            {getRequestIcon(requestStatus)}
        </div>
    );
};

const RequestStateFlow: React.FC<RequestStateProps> = ({ item }) => {
    const requestStates = getRequestStates(item);
    if (!requestStates) {
        return <div>Unknown</div>;
    }
    return (
        <div className={styles.requestStateFlow}>
            {requestStates.map((status, i) => (
                <RequestItem requestStatus={status} index={i} />
            ))}
        </div>
    );
};

export default RequestStateFlow;
