import * as React from 'react';
import { Button } from '@equinor/fusion-components';
import { useCurrentContext, combineUrls } from '@equinor/fusion';
import * as styles from '../styles.less';
import { Link } from 'react-router-dom';

type ContractLinkColumnProps = { contractId: string | null };
const ContractLinkColumn: React.FC<ContractLinkColumnProps> = ({ contractId, children }) => {
    const currentContext = useCurrentContext();
    if (!currentContext || !contractId) {
        return null;
    }

    return (
        <Link className={styles.linkInColumn} to={combineUrls(currentContext.id, contractId)}>
            {children}
        </Link>
        // <div
        //     style={{
        //         maxWidth: 'calc(var(--grid-unit) * 60px)',
        //         whiteSpace: 'nowrap',
        //         overflow: 'hidden',
        //         textOverflow: 'ellipsis',
        //     }}
        // >
        //     <Button frameless relativeUrl={combineUrls(currentContext.id, contractId)}>
        //         {children}
        //     </Button>
        // </div>
    );
};

export default ContractLinkColumn;
