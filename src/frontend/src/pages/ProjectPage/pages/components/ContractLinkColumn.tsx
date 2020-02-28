import * as React from 'react';
import {
    useCurrentContext,
    combineUrls,
} from '@equinor/fusion';
import { Button } from '@equinor/fusion-components';

type ContractLinkColumnProps = { contractId: string | null };
const ContractLinkColumn: React.FC<ContractLinkColumnProps> = ({ contractId, children }) => {
    const currentContext = useCurrentContext();
    if (!currentContext || !contractId) {
        return null;
    }

    return (
        <Button frameless relativeUrl={combineUrls(currentContext.id, contractId)}>
            {children}
        </Button>
    );
};

export default ContractLinkColumn;