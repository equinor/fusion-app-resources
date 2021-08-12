import { PersonCard, Chip } from '@equinor/fusion-components';
import Personnel from '../../../../../../models/Personnel';
import styles from './styles.less';
import classNames from 'classnames';
import { FC } from 'react';
import AzureAdStatusIndicator from '../AzureAdStatusIndicator';

type CompactPersonDetailsProps = {
    personnel: Personnel;
    originalPersonnel?: Personnel;
};
const CompactPersonDetails: FC<CompactPersonDetailsProps> = ({ personnel, originalPersonnel }) => {
    return (
        <>
            <div className={styles.compactPersonDetails}>
                <PersonCard personId={personnel.azureUniquePersonId} />
                <div className={styles.textField}>
                    <span className={styles.title}>{'AD status'}</span>
                    <span className={styles.content}>
                        <AzureAdStatusIndicator status={personnel.azureAdStatus || 'NoAccount'} />
                    </span>
                </div>
                <div className={styles.textField}>
                    <span className={styles.title}>{'Disciplines'}</span>
                    <div className={styles.content}>
                        {personnel.disciplines.map((discipline) => (
                            <Chip title={discipline.name} />
                        ))}
                    </div>
                </div>
            </div>
            {originalPersonnel && originalPersonnel.mail !== personnel.mail ? (
                <div className={classNames(styles.compactPersonDetails, styles.original)}>
                    <PersonCard personId={originalPersonnel.azureUniquePersonId} />
                    <div className={styles.textField}>
                        <span className={styles.title}>{'AD status'}</span>
                        <span className={styles.content}>
                            <AzureAdStatusIndicator
                                status={originalPersonnel.azureAdStatus || 'NoAccount'}
                            />
                        </span>
                    </div>
                    <div className={styles.textField}>
                        <span className={styles.title}>{'Disciplines'}</span>
                        <div className={styles.content}>
                            {originalPersonnel.disciplines.map((discipline) => (
                                <Chip title={discipline.name} />
                            ))}
                        </div>
                    </div>
                </div>
            ) : null}
        </>
    );
};

export default CompactPersonDetails;
