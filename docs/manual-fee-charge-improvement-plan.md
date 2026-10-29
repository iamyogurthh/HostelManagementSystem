# Manual Fee Charge Improvement Plan

## Current Status

- `ViewModels/PaymentViewModels/ManualChargeViewModel.cs` has been updated with:
  - Server-side student search.
  - Maximum 50 student results.
  - Search by student name or NRC.
  - Active fee-rule loading.
  - Fee-rule selection that copies `MonthlyAmount` into the editable `Amount` property.
- `Views/PaymentViews/ManualChargeView.xaml` still needs to be updated successfully.
- License service and license tests must remain untouched.

## Step 1: Verify the ViewModel

Review:

```text
ViewModels/PaymentViewModels/ManualChargeViewModel.cs
Models/Student.cs
Models/FeeRule.cs
```

Confirm these properties and commands exist:

```text
StudentSearchText
Students
SelectedStudent
FeeRules
SelectedFeeRule
SearchStudentsCommand
```

The student query should remain server-filtered and bounded:

```csharp
db.Students
	.AsNoTracking()
	.Where(student => student.IsActive)
	.Where(student =>
		EF.Functions.Like(student.Name, $"%{search}%") ||
		EF.Functions.Like(student.NRC, $"%{search}%"))
	.OrderBy(student => student.Name)
	.Take(50)
```

Do not load all students into memory or into a ComboBox.

Selecting a fee rule should populate `Amount`, but the amount must remain editable.

## Step 2: Replace the Student ComboBox

Modify:

```text
Views/PaymentViews/ManualChargeView.xaml
```

Replace the existing student ComboBox with a search field and bounded result list:

```xml
<Grid>
	<Grid.RowDefinitions>
		<RowDefinition Height="Auto" />
		<RowDefinition Height="120" />
	</Grid.RowDefinitions>

	<Grid.ColumnDefinitions>
		<ColumnDefinition Width="*" />
		<ColumnDefinition Width="Auto" />
	</Grid.ColumnDefinitions>

	<TextBox
		Height="40"
		Padding="10,0"
		VerticalContentAlignment="Center"
		Text="{Binding StudentSearchText, UpdateSourceTrigger=PropertyChanged}" />

	<Button
		Grid.Column="1"
		Height="40"
		Margin="8,0,0,0"
		Padding="16,0"
		Command="{Binding SearchStudentsCommand}"
		Content="ရှာမည်"
		Style="{StaticResource SecondaryButtonStyle}" />

	<ListBox
		Grid.Row="1"
		Grid.ColumnSpan="2"
		Margin="0,8,0,0"
		ItemsSource="{Binding Students}"
		SelectedItem="{Binding SelectedStudent, Mode=TwoWay}">
		<ListBox.ItemTemplate>
			<DataTemplate>
				<StackPanel Margin="4,2">
					<TextBlock FontWeight="SemiBold" Text="{Binding Name}" />
					<TextBlock
						Foreground="{DynamicResource TextSecondary}"
						Text="{Binding NRC}" />
				</StackPanel>
			</DataTemplate>
		</ListBox.ItemTemplate>
	</ListBox>
</Grid>
```

Use this label above the control:

```xml
<TextBlock
	Margin="0,0,0,6"
	FontWeight="SemiBold"
	Foreground="{DynamicResource TextPrimary}"
	Text="ကျောင်းသား *" />
```

## Step 3: Add Fee-Rule Selection Beside Amount

Replace the amount-only section with a two-column Grid:

```xml
<Grid Margin="0,0,0,16">
	<Grid.ColumnDefinitions>
		<ColumnDefinition Width="*" />
		<ColumnDefinition Width="*" />
	</Grid.ColumnDefinitions>

	<StackPanel Grid.Column="0" Margin="0,0,8,0">
		<TextBlock
			Margin="0,0,0,6"
			FontWeight="SemiBold"
			Foreground="{DynamicResource TextPrimary}"
			Text="အခကြေးငွေစည်းမျဉ်း (ရွေးချယ်နိုင်သည်)" />

		<ComboBox
			Height="40"
			ItemsSource="{Binding FeeRules}"
			SelectedItem="{Binding SelectedFeeRule, Mode=TwoWay}">
			<ComboBox.ItemTemplate>
				<DataTemplate>
					<TextBlock>
						<Run Text="{Binding StudentCategory.Name}" />
						<Run Text=" / " />
						<Run Text="{Binding RoomType.Name}" />
						<Run Text=" - " />
						<Run Text="{Binding MonthlyAmount, StringFormat={}{0:N0}}" />
					</TextBlock>
				</DataTemplate>
			</ComboBox.ItemTemplate>
		</ComboBox>
	</StackPanel>

	<StackPanel Grid.Column="1" Margin="8,0,0,0">
		<TextBlock
			Margin="0,0,0,6"
			FontWeight="SemiBold"
			Foreground="{DynamicResource TextPrimary}"
			Text="စုစုပေါင်းအခကြေးငွေ *" />

		<TextBox
			Height="40"
			Padding="10,0"
			VerticalContentAlignment="Center"
			Text="{Binding Amount, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
	</StackPanel>
</Grid>
```

## Step 4: Align the Form Layout

Use these existing forms as references:

```text
Views/FeeRuleViews/FeeRuleFormView.xaml
Views/StudentCategoryViews/StudentCategoryFormView.xaml
```

The manual charge form should include:

1. Back button and title in a horizontal header.
2. Subtitle below the title.
3. Divider below the header.
4. Error message inside a styled error border.
5. Consistent field labels.
6. Consistent vertical margins, preferably `16` or `20`.
7. Form content inside a `ScrollViewer`.
8. Consistent primary and secondary action buttons.
9. An information card explaining that:
   - The manual charge is one total amount.
   - It applies to the selected date range.
   - Unpaid overlapping charges are replaced.
   - Paid charges cannot be replaced.

Suggested root structure:

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto">
	<Grid MaxWidth="720" Margin="0,0,16,0">
		<!-- Header -->
		<!-- Divider -->
		<!-- Error -->
		<!-- Student search -->
		<!-- Fee rule and amount -->
		<!-- Dates -->
		<!-- Description -->
		<!-- Information -->
		<!-- Actions -->
	</Grid>
</ScrollViewer>
```

## Step 5: Preserve Existing Bindings

Do not remove or rename:

```text
PeriodStart
PeriodEnd
DueDate
Amount
Description
SaveCommand
CancelCommand
HasError
ErrorMessage
```

Add or preserve:

```text
StudentSearchText
SearchStudentsCommand
Students
SelectedStudent
FeeRules
SelectedFeeRule
```

## Step 6: Validate the XAML and C#

Run:

```powershell
dotnet build
```

Fix all errors and warnings. Check especially for:

- Missing `Student.NRC` property.
- Missing `StudentCategory.Name`.
- Missing `RoomType.Name`.
- Incorrect resource names such as `PrimaryButtonStyle`, `SecondaryButtonStyle`, `TextBoxStyle`, or `BooleanToVisibilityConverter`.
- XAML binding syntax errors.
- Nullable navigation-property warnings.
- CommunityToolkit generated property and command naming issues.

## Step 7: Run Focused Tests

Run:

```powershell
dotnet test HostelManagementSystem.Tests/HostelManagementSystem.Tests.csproj --filter "FullyQualifiedName~Payment"
```

Do not modify license tests or license service code.

Known unrelated issue:

- License tests may fail with external `License not found` responses.
- Treat those failures as out of scope unless caused by the new changes.

## Final Verification Checklist

- [ ] Student search does not load all students.
- [ ] Maximum 50 student results are returned.
- [ ] Search works by name and NRC.
- [ ] User can select a student from search results.
- [ ] User can type an amount manually.
- [ ] User can select an active fee rule.
- [ ] Fee-rule selection populates the amount.
- [ ] User can edit the populated amount.
- [ ] Manual charge remains one total charge for the selected date range.
- [ ] The form visually matches the other application forms.
- [ ] License service and license tests were not changed.
- [ ] Build succeeds without warnings.
- [ ] Focused payment tests pass.
