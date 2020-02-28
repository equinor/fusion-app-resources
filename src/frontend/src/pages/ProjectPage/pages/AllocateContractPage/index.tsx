import * as React from 'react';
import EditContractWizard from '../../components/EditContractWizard';
import { ArrowBackIcon, IconButton } from '@equinor/fusion-components';
import * as styles from "./styles.less";

const AllocateContractPage = () => {
    return (
        <div className={styles.container}>
            <h2><IconButton><ArrowBackIcon /></IconButton> Allocate a Contract</h2>
            <EditContractWizard />
        </div>
    );
};

export default AllocateContractPage;
