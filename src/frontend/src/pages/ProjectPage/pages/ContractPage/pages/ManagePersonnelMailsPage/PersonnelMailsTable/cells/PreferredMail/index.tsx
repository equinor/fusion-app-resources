import { CloseCircleIcon, styling, useTooltipRef } from '@equinor/fusion-components';
import { clsx } from '@equinor/fusion-react-styles';
import { ChangeEvent, FC, useCallback, useMemo } from 'react';
import Personnel from '../../../../../../../../../models/Personnel';
import { useManagePersonnelMailContext } from '../../../ManagePersonnelMailContext';
import * as styles from './styles.less';

type PreferredMailProps = {
    item: Personnel;
};

const PreferredMail: FC<PreferredMailProps> = ({ item }) => {
    const { updateContactMail, contactMailForm, showInputErrors, isSavingContactMails } =
        useManagePersonnelMailContext();

    const contatFormItem = useMemo(
        () => contactMailForm.find((formItem) => formItem.personnelId === item.personnelId),
        [item, contactMailForm]
    );

    const inputError = useMemo(() => contatFormItem?.inputError, [contatFormItem]);

    const invalidMailTooltip = useTooltipRef(inputError, 'left');
    const input = useMemo(() => contatFormItem?.preferredContactMail || '', [contatFormItem]);

    const onPreferredMailChange = useCallback(
        (input: ChangeEvent<HTMLInputElement>) => {
            if (isSavingContactMails) {
                return;
            }
            const inputValue = input.target.value;
            updateContactMail(item.personnelId, inputValue);
        },
        [updateContactMail, isSavingContactMails]
    );
    const validationError = useMemo(
        () => contatFormItem?.inputError && showInputErrors,
        [showInputErrors, contatFormItem]
    );
    const inputClasses = clsx(styles.mailInput, {
        [styles.disabled]: isSavingContactMails,
    });

    return (
        <div className={styles.container}>
            <input className={inputClasses} value={input} onChange={onPreferredMailChange} />
            <div className={styles.errorContainer}>
                {validationError && (
                    <div className={styles.error} ref={invalidMailTooltip}>
                        <CloseCircleIcon color={styling.colors.red} />
                    </div>
                )}
            </div>
        </div>
    );
};

export default PreferredMail;
