import * as React from 'react';
import EditContractWizard from '../../components/EditContractWizard';
import { useHistory, useCurrentContext } from '@equinor/fusion';
import Contract from '../../../../models/contract';

const AllocateContractPage = () => {
    const history = useHistory();
    const currentContext = useCurrentContext();

    const onCancel = React.useCallback(() => {
        history.push('/' + (currentContext?.id || ''));
    }, [history, currentContext]);

    const onGoBack = React.useCallback(() => {
        history.goBack();
    }, [history]);

    const onSubmit = React.useCallback((contract: Contract) => {
        history.push(`/${currentContext}/${contract.id}`);
    }, [currentContext, history]);

    return (
        <EditContractWizard
            title="Allocate a Contract"
            onCancel={onCancel}
            goBackTo='contracts'
            onGoBack={onGoBack}
            onSubmit={onSubmit}
        />
    );
};

export default AllocateContractPage;
