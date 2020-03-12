import * as React from 'react';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import * as styles from './styles.less';
import { PositionCard, PersonCard } from '@equinor/fusion-components';
import classNames from 'classnames';
import RequestStateFlow from '../RequestStateFlow';

type RequestDetailsProps = {
    request: PersonnelRequest;
};
const RequestDetails: React.FC<RequestDetailsProps> = ({ request }) => {
    const createItemField = React.useCallback(
        (fieldName: string, title: string, content: () => any) => {
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
            {createItemField('description', 'Description', () => request.description)}
            {createItemField(
                'basePosition',
                'Base position',
                () => request.position?.basePosition|| 'TBN'
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
            {createItemField('taskManager', 'Task Manager', () => 'TBN')}
            {createItemField('fromDate', 'From Date', () => 'TBN')}
            {createItemField('toDate', 'To Date', () => request)}
            {createItemField('person', 'Assigned person', () => <PersonCard personId={request.person?.azureUniquePersonId} />)}
            {createItemField('status', 'Status', () => (
                <RequestStateFlow item={request} />
            ))}
        </div>
    );
};

export default RequestDetails;
