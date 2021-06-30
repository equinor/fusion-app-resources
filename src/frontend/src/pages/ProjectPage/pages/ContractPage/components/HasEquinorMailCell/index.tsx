import { CheckCircleIcon, CloseCircleIcon, styling } from '@equinor/fusion-components';
import { FC } from 'react';
import Personnel from '../../../../../../models/Personnel';
import styles from './styles.less';

type HasEquinorMailCellProps = {
    item: Personnel;
};
const HasEquinorMailCell: FC<HasEquinorMailCellProps> = ({ item }) => {
    if (item.mail) {
        return (
            <div className={styles.icon}>
                <CheckCircleIcon color={styling.colors.green} />
            </div>
        );
    }

    return (
        <div className={styles.icon}>
            <CloseCircleIcon color={styling.colors.red} />
        </div>
    );
};
export default HasEquinorMailCell;
