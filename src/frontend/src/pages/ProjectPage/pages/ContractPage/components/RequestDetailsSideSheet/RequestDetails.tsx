
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import styles from './styles.less';
import classNames from 'classnames';
import { formatDate } from '@equinor/fusion';
import PositionIdCard from './PositionIdCard';
import { ReactNode, FC } from 'react';

type RequestDetailsProps = {
    request: PersonnelRequest;
};

type ItemFieldProps = {
    fieldName: string;
    title: string;
    original?: ReactNode;
    value?: string | null;
    originalValue?: string | null;
};

const ItemField: FC<ItemFieldProps> = ({ fieldName, title, original, value, originalValue, children }) => (
    <div className={classNames(styles.textField, styles[fieldName])}>
        <span className={styles.title}>{title}</span>
        <span className={styles.content}>{children}</span>
        {original && originalValue !== value ? (
            <span className={classNames(styles.content, styles.original)}>{original}</span>
        ) : null}
    </div>
);

const RequestDetails: FC<RequestDetailsProps> = ({ request }) => {
    return (
        <div className={styles.requestDetails}>
            <ItemField fieldName="description" title="Request description">
                {request.description || 'N/A'}
            </ItemField>
            <ItemField
                fieldName="basePosition"
                title="Base position"
                original={request.originalPosition?.basePosition?.name}
                value={request.position?.basePosition?.name}
                originalValue={request.originalPosition?.basePosition?.name}
            >
                {request.position?.basePosition?.name || 'N/A'}
            </ItemField>
            <ItemField
                fieldName="customPosition"
                title="Custom position title"
                original={request.originalPosition?.name}
                value={request.position?.name}
                originalValue={request.originalPosition?.name}
            >
                {request.position?.name || 'N/A'}
            </ItemField>
            <ItemField
                fieldName="taskOwner"
                title="Task owner"
                value={request.position?.taskOwner?.positionId}
                originalValue={request.originalPosition?.taskOwner?.positionId}
                original={
                    request.originalPosition?.taskOwner?.positionId ? (
                        <PositionIdCard
                            positionId={request.originalPosition.taskOwner.positionId}
                        />
                    ) : null
                }
            >
                <PositionIdCard positionId={request.position?.taskOwner?.positionId || undefined} />
            </ItemField>
            <ItemField
                fieldName="fromDate"
                title="From date"
                value={request.position?.appliesFrom ? formatDate(request.position.appliesFrom) : 'N/A'}
                originalValue={request.originalPosition?.appliesFrom ? formatDate(request.originalPosition.appliesFrom) : 'N/A'}
                original={
                    request.originalPosition?.appliesFrom
                        ? formatDate(request.originalPosition?.appliesFrom)
                        : undefined
                }
            >
                {request.position?.appliesFrom ? formatDate(request.position.appliesFrom) : 'N/A'}
            </ItemField>
            <ItemField
                fieldName="toDate"
                title="To date"
                value={request.position?.appliesTo ? formatDate(request.position.appliesTo) : 'N/A'}
                originalValue={request.originalPosition?.appliesTo ? formatDate(request.originalPosition.appliesTo) : 'N/A'}
                original={
                    request.originalPosition?.appliesTo
                        ? formatDate(request.originalPosition?.appliesTo)
                        : undefined
                }
            >
                {request.position?.appliesTo ? formatDate(request.position.appliesTo) : 'N/A'}
            </ItemField>
            <ItemField
                fieldName="workload"
                title="Workload"
                value={request.position?.workload?.toString()}
                originalValue={request.originalPosition?.workload?.toString()}
                original={
                    request.originalPosition
                        ? request.originalPosition?.workload.toString() + '%'
                        : undefined
                }
            >
                {request.position?.workload.toString() + '%' || 'N/A'}
            </ItemField>
        </div>
    );
};

export default RequestDetails;
