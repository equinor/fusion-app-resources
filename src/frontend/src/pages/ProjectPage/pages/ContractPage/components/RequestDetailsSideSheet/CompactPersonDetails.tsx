import * as React from 'react';
import { PersonCard, Chip } from '@equinor/fusion-components';
import Personnel from '../../../../../../models/Personnel';
import * as styles from './styles.less';
import AzureAdStatusIcon from '../../pages/ManagePersonnelPage/components/AzureAdStatus';

type CompactPersonDetailsProps = {
    personnel: Personnel;
};
const CompactPersonDetails: React.FC<CompactPersonDetailsProps> = ({ personnel }) => {
    return (
        <div className={styles.compactPersonDetails}>
            <PersonCard personId={personnel.azureUniquePersonId} />
            <div className={styles.textField}>
                <span className={styles.title}>{'AD status'}</span>
                <span className={styles.content}>
                    {AzureAdStatusIcon(personnel.azureAdStatus || 'NoAccount')}
                </span>
            </div>
            <div className={styles.textField}>
                <span className={styles.title}>{'Disciplines'}</span>
                <div className={styles.content}>
                    {personnel.disciplines.map(discipline => (
                        <Chip title={discipline.name} />
                    ))}
                </div>
            </div>
        </div>
    );
};

export default CompactPersonDetails;
