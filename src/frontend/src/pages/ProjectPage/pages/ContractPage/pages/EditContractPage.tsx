import * as React from 'react';
import EditContractWizard from '../../../components/EditContractWizard';
import { useHistory } from '@equinor/fusion';
import useContractFromId from '../hooks/useContractFromId';
import { RouteComponentProps } from 'react-router-dom';

type EditContractPageMatch = {
    contractId: string;
};

type EditContractPageProps = RouteComponentProps<EditContractPageMatch>;

const EditContractPage: React.FC<EditContractPageProps> = ({ match }) => {
    const { contract, isFetchingContract } = useContractFromId(match.params.contractId);
    const history = useHistory();

    const goBack = React.useCallback(() => history.goBack(), [history]);

    if (isFetchingContract) {
        return null;
    }

    return (
        <EditContractWizard
            goBackTo="contract details"
            onCancel={goBack}
            onGoBack={goBack}
            title={`Edit ${contract?.name}`}
            existingContract={contract || undefined}
        />
    );
};

export default EditContractPage;
