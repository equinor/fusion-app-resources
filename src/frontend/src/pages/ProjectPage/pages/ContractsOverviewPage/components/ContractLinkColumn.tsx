
import { useCurrentContext, combineUrls } from '@equinor/fusion';
import * as styles from '../styles.less';
import { Link } from 'react-router-dom';
import { FC } from 'react';

type ContractLinkColumnProps = { contractId: string | null };
const ContractLinkColumn: FC<ContractLinkColumnProps> = ({ contractId, children }) => {
    const currentContext = useCurrentContext();
    if (!currentContext || !contractId) {
        return null;
    }

    return (
        <Link className={styles.linkInColumn} to={combineUrls(currentContext.id, contractId)}>
            {children}
        </Link>
    );
};

export default ContractLinkColumn;
