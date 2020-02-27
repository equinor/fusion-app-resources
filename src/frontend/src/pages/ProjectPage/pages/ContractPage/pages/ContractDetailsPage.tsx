import * as React from 'react';
import { useContractContext } from '../../../../../contractContex';

const ContractDetailsPage = () => {
    const contractContext = useContractContext();

    if (!contractContext) {
        return null;
    }

    return (
        <div>
            <h1>{contractContext.contract.id}</h1>
            <h2>Details</h2>
        </div>
    );
};

export default ContractDetailsPage;
