import React, { forwardRef } from 'react';
import { CheckBox } from '@equinor/fusion-components';
import classNames from 'classnames';
import styles from './styles.less';

export type SelectionCellProps = {
  isSelectable: boolean;
  isSelected: boolean;
  onChange: () => void;
  isHovering?: boolean;
  indeterminate?: boolean;
};

const SelectionCell = forwardRef<
  HTMLDivElement | null,
  React.PropsWithChildren<SelectionCellProps>
>(
  (
    {
      isSelectable,
      isSelected,
      onChange,
      isHovering,
      indeterminate,
    },
    ref
  ) => {
    return (
      <div
        className={styles.cell}
        ref={ref as React.MutableRefObject<HTMLDivElement | null>}
      >
        {isSelectable && (
          <CheckBox
            selected={isSelected}
            onChange={onChange}
            indeterminate={indeterminate}
          />
        )}
      </div>
    );
  }
);

export default SelectionCell;
