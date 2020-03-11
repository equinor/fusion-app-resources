import * as React from 'react';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import * as styles from './styles.less';
type RequestDetailsProps = {
    request: PersonnelRequest;
};
const RequestDetails: React.FC<RequestDetailsProps> = ({ request }) => {
    const createTextField = React.useCallback(
        (fieldName: string, title: string, content: string) => {
            return (
                <div className={styles[fieldName]}>
                    <span className={styles.title}>{title}</span>
                    <span className={styles.content}>{content}</span>
                </div>
            );
        },
        []
    );

    return (
        <div className={styles.requestDetails}>
            {createTextField('description', 'Description', request.description)}
        </div>
    );
};

export default RequestDetails;
