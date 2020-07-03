import * as React from 'react';
import * as styles from './styles.less';
import CertifyToPicker from '../../../CertifiyToPicker';
import { Button, SyncIcon } from '@equinor/fusion-components';

type CertifyToPopoverProps ={
    canEdit: boolean;
}

const CertifyToPopover: React.FC<CertifyToPopoverProps> = ({canEdit}) => {
    const [selectedDate, setSelectedDate] = React.useState<Date | null>(null);
    
    return (
        <div className={styles.container}>
            <CertifyToPicker onChange={setSelectedDate} defaultSelected="12-months" isReCertification/>
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
