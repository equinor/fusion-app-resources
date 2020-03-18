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
import PersonnelRequest from '../../../../../models/PersonnelRequest';

type RequestStateProps = {
    item: PersonnelRequest;
};

type RequestItemState = 'approved' | 'pending' | 'rejected';

type RequestItemProps = {
    requestStatus: RequestItemState;
    index: number;
};

const states = ['Created state', 'Contractor state', 'Company state'];

const getRequestStates = (request: PersonnelRequest): RequestItemState[] | null => {
    const state = request.state;

    switch (state) {
        case 'Created':
            return ['approved', 'pending', 'pending'];
        case 'SubmittedToCompany':
            return ['approved', 'approved', 'pending'];
        case 'RejectedByContractor':
            return ['approved', 'rejected', 'pending'];
        case 'ApprovedByCompany':
            return ['approved', 'approved', 'approved'];
        case 'RejectedByCompany':
            return ['approved', 'approved', 'rejected'];
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
                <RequestItem key={i.toString()} requestStatus={status} index={i} />
            ))}
        </div>
    );
};

export default RequestStateFlow;
