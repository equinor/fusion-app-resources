import * as React from 'react';
import * as styles from './styles.less';
import CertifyToPicker from '../../../CertifiyToPicker';
import { Button, SyncIcon } from '@equinor/fusion-components';

const CertifyToPopover: React.FC = () => {
    const [selectedDate, setSelectedDate] = React.useState<Date | null>(null);

    return (
        <div className={styles.container}>
            <CertifyToPicker onChange={setSelectedDate} defaultSelected="12-months" />
            <div className={styles.certifyButtonContainer}>
                <Button>
                    <div className={styles.syncButton}>
                        <SyncIcon />
                        <span>Re-Certify</span>
                    </div>
                </Button>
            </div>
        </div>
    );
};

export default CertifyToPopover;
