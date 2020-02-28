import * as React from 'react';
import EditContractWizard from '../../components/EditContractWizard';
import { ArrowBackIcon, IconButton } from '@equinor/fusion-components';
import * as styles from "./styles.less";

const AllocateContractPage = () => {
    return (
        <div className={styles.container}>
            <EditContractWizard title="Allocate a Contract" />
        </div>
    );
};

export default AllocateContractPage;
