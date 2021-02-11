import { useCallback } from 'react';
import EditContractWizard from '../../components/EditContractWizard';
import { useHistory, useCurrentContext } from '@equinor/fusion';
import Contract from '../../../../models/contract';

const AllocateContractPage = () => {
    const history = useHistory();
    const currentContext = useCurrentContext();

    const onCancel = useCallback(() => {
        history.push('/' + (currentContext?.id || ''));
    }, [history, currentContext]);

    const onGoBack = useCallback(() => {
        history.goBack();
    }, [history]);

    const onSubmit = useCallback((contract: Contract) => {
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
