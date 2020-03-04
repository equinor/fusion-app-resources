import * as React from 'react';
import EditContractWizard, { ContractWizardSkeleton } from '../../../components/EditContractWizard';
import { useHistory, useCurrentContext } from '@equinor/fusion';
import useContractFromId from '../hooks/useContractFromId';
import { RouteComponentProps } from 'react-router-dom';
import Contract from '../../../../../models/contract';

type EditContractPageMatch = {
    contractId: string;
};

type EditContractPageProps = RouteComponentProps<EditContractPageMatch>;

const EditContractPage: React.FC<EditContractPageProps> = ({ match }) => {
    const { contract, isFetchingContract } = useContractFromId(match.params.contractId);
    const history = useHistory();

    const goBack = React.useCallback(() => history.goBack(), [history]);

    const currentContext = useCurrentContext();
    const onSubmit = React.useCallback((updatedContract: Contract) => {
        history.push(`/${currentContext}/${updatedContract.id}`);
    }, [history, currentContext]);

    if (isFetchingContract) {
        return <ContractWizardSkeleton isEdit />;
    }

    return (
        <EditContractWizard
            goBackTo="contract details"
            onCancel={goBack}
            onGoBack={goBack}
            title={`Edit ${contract?.name}`}
            existingContract={contract || undefined}
            onSubmit={onSubmit}
        />
    );
};

export default EditContractPage;
