import * as React from 'react';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import * as styles from './styles.less';
import { PersonCard } from '@equinor/fusion-components';
import classNames from 'classnames';
import { formatDate } from '@equinor/fusion';
import PositionIdCard from './PositionIdCard';

type RequestDetailsProps = {
    request: PersonnelRequest;
};
const RequestDetails: React.FC<RequestDetailsProps> = ({ request }) => {
    const createItemField = React.useCallback(
        (fieldName: string, title: string, content: () => string | JSX.Element) => {
            return (
                <div className={classNames(styles.textField, styles[fieldName])}>
                    <span className={styles.title}>{title}</span>
                    <span className={styles.content}>{content()}</span>
                </div>
            );
        },
        []
    );

    return (
        <div className={styles.requestDetails}>
            {createItemField('description', 'Description', () => request.description || 'N/A')}
            {createItemField(
                'basePosition',
                'Base position',
                () => request.position?.basePosition?.name || 'TBN'
            )}
            {createItemField(
                'customPosition',
                'Custom position title',
                () => request.position?.name || 'TBN'
            )}
            {createItemField(
                'customPosition',
                'Custom position title',
                () => request.position?.name || 'TBN'
            )}
            {createItemField('taskOwner', 'Task Owner', () => (
                <PositionIdCard positionId={request.position?.taskOwner?.positionId || undefined} />
            ))}
            {createItemField('fromDate', 'From Date', () =>
                request.position?.appliesFrom ? formatDate(request.position.appliesFrom) : 'TBN'
            )}
            {createItemField('toDate', 'To Date', () =>
                request.position?.appliesTo ? formatDate(request.position.appliesTo) : 'TBN'
            )}
            {createItemField(
                'workload',
                'Workload',
                () => request.position?.workload.toString() + '%' || 'N/A'
            )}
        </div>
    );
};

export default RequestDetails;
