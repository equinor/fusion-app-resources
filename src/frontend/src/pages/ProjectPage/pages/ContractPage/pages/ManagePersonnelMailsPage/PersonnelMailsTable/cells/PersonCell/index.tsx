import { PersonPhoto } from '@equinor/fusion-components';
import { FC } from 'react';
import Personnel from '../../../../../../../../../models/Personnel';
import styles from './styles.less';

type PersonCellProps = {
    item: Personnel;
};
const PersonCell: FC<PersonCellProps> = ({ item }) => {
    return (
        <div data-cy="person-column" className={styles.container}>
            {item.azureUniquePersonId ? (
                <PersonPhoto size="small" personId={item.azureUniquePersonId} hideTooltip />
            ) : null}
            <div className={styles.personDetails}>
                <span className={styles.name}>{item.name}</span>
                <a href={`mailto: ${item.mail}`} className={styles.mail} tabIndex={-1}>
                    {item.mail || 'No mail'}
                </a>
            </div>
        </div>
    );
};
export default PersonCell;
