import { PersonPhoto } from '@equinor/fusion-components';
import { FC } from 'react';
import Personnel from '../../../../../../../../../models/Personnel';
import styles from './styles.less';

type PersonCellProps = {
    item: Personnel;
};
const PersonCell: FC<PersonCellProps> = ({ item }) => {
    return (
        <div className={styles.container}>
            {item.azureUniquePersonId ? (
                <PersonPhoto size="small" personId={item.azureUniquePersonId} hideTooltip />
            ) : null}
            <span className={styles.name}>{item.name}</span>
        </div>
    );
};
export default PersonCell;
