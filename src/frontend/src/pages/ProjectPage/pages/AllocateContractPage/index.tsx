import * as React from 'react';
import EditContractWizard from '../../components/EditContractWizard';
import * as styles from './styles.less';
import { useHistory, useCurrentContext } from '@equinor/fusion';

const AllocateContractPage = () => {
    const history = useHistory();
    const currentContext = useCurrentContext();

    const onCancel = React.useCallback(() => {
        history.push(currentContext?.id || '');
    }, [history, currentContext]);

    const onGoBack = React.useCallback(() => {
        history.goBack();
    }, [history]);

    return (
        <div className={styles.container}>
            <EditContractWizard
                title="Allocate a Contract"
                onCancel={onCancel}
                goBackTo='contracts'
                onGoBack={onGoBack}
            />
        </div>
    );
};

export default AllocateContractPage;
