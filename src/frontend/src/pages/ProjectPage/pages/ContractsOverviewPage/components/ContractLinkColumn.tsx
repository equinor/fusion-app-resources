
import { useCurrentContext, combineUrls } from '@equinor/fusion';
import styles from '../styles.less';
import { Link } from 'react-router-dom';
import { FC } from 'react';

type ContractLinkColumnProps = { contractId: string | null };
const ContractLinkColumn: FC<ContractLinkColumnProps> = ({ contractId, children, ...props }) => {
    const currentContext = useCurrentContext();
    if (!currentContext || !contractId) {
        return null;
    }

    return (
        <Link {...props} className={styles.linkInColumn} to={combineUrls(currentContext.id, contractId)}>
            {children}
        </Link>
    );
};

export default ContractLinkColumn;
